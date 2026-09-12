using ClinicManagement.Application.Encounters;
using ClinicManagement.Application.Encounters.Dtos;
using ClinicManagement.Application.Queue;
using ClinicManagement.Domain.Appointments;
using ClinicManagement.Domain.Doctors;
using ClinicManagement.Domain.Patients;
using ClinicManagement.Domain.Queue;
using UnitTests.Common;

namespace UnitTests.Encounters;

/// <summary>
/// Tự xử lý vé hàng đợi khi chốt phiếu khám — tránh số ảo treo mãi trên bảng hàng đợi, đặc biệt khi bác
/// sĩ tự "Bắt đầu khám" (UX-05) bỏ qua hẳn bước gọi số của Lễ tân/Điều dưỡng.
/// </summary>
public sealed class EncounterAutoResolveQueueTicketTests
{
    private static readonly DateTimeOffset Base = new(2026, 8, 22, 8, 0, 0, TimeSpan.Zero);

    private static EncounterService SetupWithQueueService(
        string dbName, out TestDbContext db, out Appointment appt, out QueueTicket ticket)
    {
        db = TestDbContext.CreateInMemory(dbName);
        var patient = new Patient("BN-000001", "Nguyễn Văn A", null, Gender.Male, null, null);
        var doctor = new Doctor("BS-000001", "BS. Trần B", Guid.NewGuid(), null, null);
        db.Patients.Add(patient);
        db.Doctors.Add(doctor);
        appt = new Appointment(patient.Id, doctor.Id, Base, Base.AddMinutes(30), null);
        appt.CheckIn();
        appt.Start();
        db.Appointments.Add(appt);
        ticket = new QueueTicket(DateOnly.FromDateTime(Base.Date), 1, patient.Id, appt.Id, null, doctor.Id);
        db.QueueTickets.Add(ticket);
        db.SaveChanges();

        return new EncounterService(db, invoices: null, visits: null, queue: new QueueService(db));
    }

    private static CreateEncounterRequest Req(Guid apptId) =>
        new(apptId, null, "Viêm họng cấp", null, Array.Empty<PrescriptionItemRequest>());

    [Fact]
    public async Task Complete_ShouldMarkTicketDone_WhenInProgress()
    {
        var service = SetupWithQueueService(
            nameof(Complete_ShouldMarkTicketDone_WhenInProgress), out var db, out var appt, out var ticket);
        ticket.Call();
        ticket.Start(); // InProgress — vé đã đi qua đúng quy trình gọi số
        await db.SaveChangesAsync();
        var created = await service.CreateAsync(Req(appt.Id));

        var completed = await service.CompleteAsync(created.Value.Id);

        Assert.True(completed.IsSuccess);
        var reloaded = await db.QueueTickets.FindAsync(ticket.Id);
        Assert.Equal(QueueTicketStatus.Done, reloaded!.Status);
    }

    [Fact]
    public async Task Complete_ShouldSkipTicket_WhenStillWaiting()
    {
        // Bác sĩ tự "Bắt đầu khám" (UX-05), bỏ qua hẳn bước gọi số — vé vẫn Waiting lúc chốt phiếu.
        var service = SetupWithQueueService(
            nameof(Complete_ShouldSkipTicket_WhenStillWaiting), out var db, out var appt, out var ticket);
        var created = await service.CreateAsync(Req(appt.Id));

        var completed = await service.CompleteAsync(created.Value.Id);

        Assert.True(completed.IsSuccess);
        var reloaded = await db.QueueTickets.FindAsync(ticket.Id);
        Assert.Equal(QueueTicketStatus.Skipped, reloaded!.Status);
    }

    [Fact]
    public async Task Complete_ShouldSkipTicket_WhenCalledButNotStarted()
    {
        var service = SetupWithQueueService(
            nameof(Complete_ShouldSkipTicket_WhenCalledButNotStarted), out var db, out var appt, out var ticket);
        ticket.Call(); // Called — chưa Start
        await db.SaveChangesAsync();
        var created = await service.CreateAsync(Req(appt.Id));

        var completed = await service.CompleteAsync(created.Value.Id);

        Assert.True(completed.IsSuccess);
        var reloaded = await db.QueueTickets.FindAsync(ticket.Id);
        Assert.Equal(QueueTicketStatus.Skipped, reloaded!.Status);
    }

    [Fact]
    public async Task Complete_ShouldNotThrow_WhenNoTicketForAppointment()
    {
        var db = TestDbContext.CreateInMemory(nameof(Complete_ShouldNotThrow_WhenNoTicketForAppointment));
        var patient = new Patient("BN-000001", "Nguyễn Văn A", null, Gender.Male, null, null);
        var doctor = new Doctor("BS-000001", "BS. Trần B", Guid.NewGuid(), null, null);
        db.Patients.Add(patient);
        db.Doctors.Add(doctor);
        var appt = new Appointment(patient.Id, doctor.Id, Base, Base.AddMinutes(30), null);
        appt.CheckIn();
        appt.Start();
        db.Appointments.Add(appt);
        db.SaveChanges();
        var service = new EncounterService(db, invoices: null, visits: null, queue: new QueueService(db));
        var created = await service.CreateAsync(Req(appt.Id));

        var completed = await service.CompleteAsync(created.Value.Id);

        Assert.True(completed.IsSuccess); // không có vé nào gắn lịch — bỏ qua, không lỗi
    }
}
