using ClinicManagement.Application.Vitals;
using ClinicManagement.Application.Vitals.Dtos;
using ClinicManagement.Domain.Appointments;
using ClinicManagement.Domain.Patients;
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

    private static UpsertVitalsRequest Request(decimal? height = 170m, decimal? weight = 68m) =>
        new(height, weight, 37.0m, 78, 120, 80, 98, 18, "Ổn định");

    [Fact]
    public async Task UpsertAsync_ShouldCreate_AndComputeBmi()
    {
        var service = CreateService(out _, out var appointmentId, out var patientId);

        var result = await service.UpsertAsync(appointmentId, Request(170m, 68m), Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal(patientId, result.Value.PatientId);
        // BMI = 68 / 1.7^2 = 23.529... → 23.5
        Assert.Equal(23.5m, result.Value.Bmi);
        Assert.Equal(120, result.Value.BloodPressureSystolic);
    }

    [Fact]
    public async Task UpsertAsync_ShouldReturnNullBmi_WhenHeightOrWeightMissing()
    {
        var service = CreateService(out _, out var appointmentId, out _);

        var result = await service.UpsertAsync(appointmentId, Request(null, 68m), Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Bmi);
    }

    [Fact]
    public async Task UpsertAsync_ShouldUpdate_ExistingVitals_OnePerAppointment()
    {
        var service = CreateService(out var db, out var appointmentId, out _);

        var first = await service.UpsertAsync(appointmentId, Request(170m, 68m), Guid.NewGuid());
        var second = await service.UpsertAsync(appointmentId, Request(170m, 70m), Guid.NewGuid());

        // Cùng một bản ghi (upsert), không tạo thêm.
        Assert.Equal(first.Value.Id, second.Value.Id);
        Assert.Single(db.Vitals);
        // BMI = 70 / 1.7^2 = 24.22 → 24.2
        Assert.Equal(24.2m, second.Value.Bmi);
    }

    [Fact]
    public async Task UpsertAsync_ShouldFail_WhenAppointmentMissing()
    {
        var service = CreateService(out _, out _, out _);

        var result = await service.UpsertAsync(Guid.NewGuid(), Request(), Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("Appointment.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task GetByAppointmentAsync_ShouldReturnNull_WhenNotMeasured()
    {
        var service = CreateService(out _, out var appointmentId, out _);

        var result = await service.GetByAppointmentAsync(appointmentId);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }
}
