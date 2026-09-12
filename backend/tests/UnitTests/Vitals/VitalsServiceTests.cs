using ClinicManagement.Application.Vitals;
using ClinicManagement.Application.Vitals.Dtos;
using ClinicManagement.Domain.Appointments;
using ClinicManagement.Domain.Patients;
using ClinicManagement.Domain.Visits;
using ClinicManagement.Shared.Results;
using UnitTests.Common;

namespace UnitTests.Vitals;

public sealed class VitalsServiceTests
{
    private static readonly DateTimeOffset Base = new(2026, 9, 12, 8, 0, 0, TimeSpan.Zero);

    private static VitalsService CreateService(out TestDbContext db, out Guid appointmentId, out Guid patientId)
    {
        db = TestDbContext.CreateInMemory();

        var patient = new Patient("BN-000001", "Nguyễn Văn A", null, Gender.Male, null, null);
        var appointment = new Appointment(patient.Id, Guid.NewGuid(), Base, Base.AddMinutes(30), "Khám");
        db.Patients.Add(patient);
        db.Appointments.Add(appointment);
        db.SaveChanges();

        appointmentId = appointment.Id;
        patientId = patient.Id;
        return new VitalsService(db);
    }

    private static RecordVitalsRequest Request(decimal? height = 170m, decimal? weight = 68m) =>
        new(height, weight, 37.0m, 78, 120, 80, 98, 18, "Ổn định");

    [Fact]
    public async Task RecordAsync_ShouldCreate_AndComputeBmi()
    {
        var service = CreateService(out _, out var appointmentId, out var patientId);

        var result = await service.RecordAsync(appointmentId, Request(170m, 68m), Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal(patientId, result.Value.PatientId);
        // BMI = 68 / 1.7^2 = 23.529... → 23.5
        Assert.Equal(23.5m, result.Value.Bmi);
        Assert.Equal(120, result.Value.BloodPressureSystolic);
    }

    [Fact]
    public async Task RecordAsync_ShouldReturnNullBmi_WhenHeightOrWeightMissing()
    {
        var service = CreateService(out _, out var appointmentId, out _);

        var result = await service.RecordAsync(appointmentId, Request(null, 68m), Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Bmi);
    }

    [Fact]
    public async Task RecordAsync_Twice_ShouldCreateTwoEntries_AndKeepHistory()
    {
        // Đo lại (bệnh nhân yêu cầu đo lại/chỉ số bất thường) → không ghi đè, giữ cả hai lần đo.
        var service = CreateService(out var db, out var appointmentId, out _);

        var first = await service.RecordAsync(appointmentId, Request(170m, 68m), Guid.NewGuid());
        var second = await service.RecordAsync(appointmentId, Request(170m, 70m), Guid.NewGuid());

        Assert.NotEqual(first.Value.Id, second.Value.Id);
        Assert.Equal(2, db.Vitals.Count());

        var latest = await service.GetLatestByAppointmentAsync(appointmentId);
        Assert.Equal(second.Value.Id, latest.Value!.Id);
        Assert.Equal(24.2m, latest.Value.Bmi); // BMI theo lần đo mới nhất (70kg)

        var history = await service.GetHistoryByAppointmentAsync(appointmentId);
        Assert.Equal(2, history.Value.Count);
        Assert.Equal(second.Value.Id, history.Value[0].Id); // mới nhất trước
        Assert.Equal(first.Value.Id, history.Value[1].Id);
    }

    [Fact]
    public async Task RecordAsync_ShouldFail_WhenAppointmentMissing()
    {
        var service = CreateService(out _, out _, out _);

        var result = await service.RecordAsync(Guid.NewGuid(), Request(), Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("Appointment.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task GetLatestByAppointmentAsync_ShouldReturnNull_WhenNotMeasured()
    {
        var service = CreateService(out _, out var appointmentId, out _);

        var result = await service.GetLatestByAppointmentAsync(appointmentId);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task GetHistoryByAppointmentAsync_ShouldReturnEmpty_WhenNotMeasured()
    {
        var service = CreateService(out _, out var appointmentId, out _);

        var result = await service.GetHistoryByAppointmentAsync(appointmentId);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    // ── Sinh hiệu gom theo Lượt tiếp đón (nhiều dịch vụ khám cùng lượt dùng chung lịch sử đo) ──

    private static VitalsService CreateServiceWithVisit(
        out TestDbContext db, out Guid apptId1, out Guid apptId2, out Guid patientId)
    {
        db = TestDbContext.CreateInMemory();

        var patient = new Patient("BN-000001", "Nguyễn Văn A", null, Gender.Male, null, null);
        var visit = new Visit("LK-000001", patient.Id, null);
        var a1 = new Appointment(patient.Id, Guid.NewGuid(), Base, Base.AddMinutes(30), null, visitId: visit.Id);
        var a2 = new Appointment(
            patient.Id, Guid.NewGuid(), Base.AddHours(1), Base.AddHours(1).AddMinutes(30), null, visitId: visit.Id);
        db.Patients.Add(patient);
        db.Visits.Add(visit);
        db.Appointments.AddRange(a1, a2);
        db.SaveChanges();

        apptId1 = a1.Id;
        apptId2 = a2.Id;
        patientId = patient.Id;
        return new VitalsService(db);
    }

    [Fact]
    public async Task RecordAsync_FromEitherAppointmentOfSameVisit_ShouldShareHistory()
    {
        var service = CreateServiceWithVisit(out var db, out var apptId1, out var apptId2, out _);

        var first = await service.RecordAsync(apptId1, Request(170m, 68m), Guid.NewGuid());
        var second = await service.RecordAsync(apptId2, Request(170m, 70m), Guid.NewGuid());

        Assert.Equal(2, db.Vitals.Count());

        // Xem từ dịch vụ khám nào trong lượt cũng thấy đúng lần đo gần nhất (bất kể đo từ dịch vụ nào).
        var latestFrom1 = await service.GetLatestByAppointmentAsync(apptId1);
        var latestFrom2 = await service.GetLatestByAppointmentAsync(apptId2);
        Assert.Equal(second.Value.Id, latestFrom1.Value!.Id);
        Assert.Equal(second.Value.Id, latestFrom2.Value!.Id);

        // Lịch sử đầy đủ cũng dùng chung, xem từ dịch vụ khám nào cũng ra 2 lần đo.
        var historyFrom2 = await service.GetHistoryByAppointmentAsync(apptId2);
        Assert.Equal(2, historyFrom2.Value.Count);
        Assert.Contains(historyFrom2.Value, v => v.Id == first.Value.Id);
        Assert.Contains(historyFrom2.Value, v => v.Id == second.Value.Id);
    }
}
