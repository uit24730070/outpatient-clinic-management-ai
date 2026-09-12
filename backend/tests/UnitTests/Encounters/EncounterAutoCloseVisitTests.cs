using ClinicManagement.Application.Encounters;
using ClinicManagement.Application.Encounters.Dtos;
using ClinicManagement.Application.Visits;
using ClinicManagement.Domain.Appointments;
using ClinicManagement.Domain.Doctors;
using ClinicManagement.Domain.Patients;
using ClinicManagement.Domain.Visits;
using UnitTests.Common;

namespace UnitTests.Encounters;

/// <summary>
/// Tự đóng lượt tiếp đón khi dịch vụ khám cuối cùng của lượt hoàn tất (chốt phiếu) — coi như bác sĩ đã
/// xong việc, chuyển bệnh nhân xuống quầy thuốc/thu ngân (thanh toán/cấp phát độc lập với VisitStatus).
/// Chỉ kích hoạt tại thời điểm chốt phiếu — Huỷ/Không đến ở lịch khác cùng lượt không tự kích hoạt lại
/// kiểm tra (vẫn còn nút "Đóng lượt" thủ công cho các trường hợp đó).
/// </summary>
public sealed class EncounterAutoCloseVisitTests
{
    private static readonly DateTimeOffset Base = new(2026, 8, 22, 8, 0, 0, TimeSpan.Zero);

    private static EncounterService SetupWithVisitService(
        string dbName, out TestDbContext db, out Visit visit, out Appointment appt1, out Appointment appt2)
    {
        db = TestDbContext.CreateInMemory(dbName);
        var patient = new Patient("BN-000001", "Nguyễn Văn A", null, Gender.Male, null, null);
        var doctor = new Doctor("BS-000001", "BS. Trần B", Guid.NewGuid(), null, null);
        db.Patients.Add(patient);
        db.Doctors.Add(doctor);

        visit = new Visit("LK-000001", patient.Id, null);
        db.Visits.Add(visit);

        appt1 = new Appointment(patient.Id, doctor.Id, Base, Base.AddMinutes(30), null, visitId: visit.Id);
        appt1.CheckIn();
        appt1.Start();
        appt2 = new Appointment(patient.Id, doctor.Id, Base.AddHours(1), Base.AddHours(1).AddMinutes(30), null, visitId: visit.Id);
        db.Appointments.AddRange(appt1, appt2);
        db.SaveChanges();

        return new EncounterService(db, invoices: null, visits: new VisitService(db));
    }

    private static CreateEncounterRequest Req(Guid apptId) =>
        new(apptId, null, "Viêm họng cấp", null, Array.Empty<PrescriptionItemRequest>());

    [Fact]
    public async Task Complete_ShouldNotCloseVisit_WhenOtherAppointmentStillPending()
    {
        var service = SetupWithVisitService(
            nameof(Complete_ShouldNotCloseVisit_WhenOtherAppointmentStillPending),
            out var db, out var visit, out var appt1, out _);
        var created = await service.CreateAsync(Req(appt1.Id));

        var completed = await service.CompleteAsync(created.Value.Id);

        Assert.True(completed.IsSuccess);
        var reloaded = await db.Visits.FindAsync(visit.Id);
        Assert.Equal(VisitStatus.Open, reloaded!.Status); // appt2 vẫn Scheduled → chưa đóng
    }

    [Fact]
    public async Task Complete_ShouldCloseVisit_WhenOtherAppointmentAlreadyTerminal()
    {
        var service = SetupWithVisitService(
            nameof(Complete_ShouldCloseVisit_WhenOtherAppointmentAlreadyTerminal),
            out var db, out var visit, out var appt1, out var appt2);
        appt2.MarkNoShow(); // bệnh nhân không đến dịch vụ thứ hai — đã dứt điểm trước khi bác sĩ chốt phiếu 1
        await db.SaveChangesAsync();
        var created = await service.CreateAsync(Req(appt1.Id));

        var completed = await service.CompleteAsync(created.Value.Id);

        Assert.True(completed.IsSuccess);
        var reloaded = await db.Visits.FindAsync(visit.Id);
        Assert.Equal(VisitStatus.Closed, reloaded!.Status);
    }

    [Fact]
    public async Task Complete_ShouldCloseVisit_WhenLastOfTwoEncountersFinishes()
    {
        var service = SetupWithVisitService(
            nameof(Complete_ShouldCloseVisit_WhenLastOfTwoEncountersFinishes),
            out var db, out var visit, out var appt1, out var appt2);
        var created1 = await service.CreateAsync(Req(appt1.Id));
        await service.CompleteAsync(created1.Value.Id);
        var afterFirst = await db.Visits.FindAsync(visit.Id);
        Assert.Equal(VisitStatus.Open, afterFirst!.Status); // appt2 vẫn InProgress/Scheduled

        appt2.CheckIn();
        appt2.Start();
        await db.SaveChangesAsync();
        var created2 = await service.CreateAsync(Req(appt2.Id));

        var completed2 = await service.CompleteAsync(created2.Value.Id);

        Assert.True(completed2.IsSuccess);
        var reloaded = await db.Visits.FindAsync(visit.Id);
        Assert.Equal(VisitStatus.Closed, reloaded!.Status);
    }

    [Fact]
    public async Task Complete_ShouldNotThrow_WhenAppointmentHasNoVisit()
    {
        var service = SetupWithVisitService(
            nameof(Complete_ShouldNotThrow_WhenAppointmentHasNoVisit),
            out var db, out _, out _, out _);
        var patient = new Patient("BN-000002", "Trần Thị C", null, Gender.Female, null, null);
        var doctor = new Doctor("BS-000002", "BS. Lẻ", Guid.NewGuid(), null, null);
        db.Patients.Add(patient);
        db.Doctors.Add(doctor);
        // Lịch lẻ, không gắn lượt tiếp đón nào (visitId = null).
        var appt = new Appointment(patient.Id, doctor.Id, Base.AddHours(3), Base.AddHours(3).AddMinutes(30), null);
        appt.CheckIn();
        appt.Start();
        db.Appointments.Add(appt);
        await db.SaveChangesAsync();
        var created = await service.CreateAsync(Req(appt.Id));

        var completed = await service.CompleteAsync(created.Value.Id);

        Assert.True(completed.IsSuccess); // không lỗi dù không có lượt để đóng
    }
}
