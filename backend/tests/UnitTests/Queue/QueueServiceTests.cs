using ClinicManagement.Application.Queue;
using ClinicManagement.Application.Queue.Dtos;
using ClinicManagement.Domain.Appointments;
using ClinicManagement.Domain.Patients;
using ClinicManagement.Domain.Queue;
using ClinicManagement.Shared.Results;
using UnitTests.Common;

namespace UnitTests.Queue;

public sealed class QueueServiceTests
{
    private static QueueService CreateService(out TestDbContext db, out Guid patientId, out Guid appointmentId)
    {
        db = TestDbContext.CreateInMemory();

        var patient = new Patient("BN-000001", "Nguyễn Văn A", null, Gender.Male, null, null);
        var appointment = new Appointment(
            patient.Id, Guid.NewGuid(),
            new DateTimeOffset(2026, 9, 12, 8, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 12, 8, 30, 0, TimeSpan.Zero), "Khám");
        db.Patients.Add(patient);
        db.Appointments.Add(appointment);
        db.SaveChanges();

        patientId = patient.Id;
        appointmentId = appointment.Id;
        return new QueueService(db);
    }

    [Fact]
    public async Task CreateAsync_ShouldAssignSequentialNumbers_PerDay()
    {
        var service = CreateService(out _, out var patientId, out var appointmentId);

        var first = await service.CreateAsync(new CreateQueueTicketRequest(patientId, appointmentId, null, null));
        var second = await service.CreateAsync(new CreateQueueTicketRequest(patientId, null, null, null));

        Assert.Equal(1, first.Value.Number);
        Assert.Equal(2, second.Value.Number);
    }

    [Fact]
    public async Task CreateAsync_ShouldSupportWalkIn_WithoutAppointment()
    {
        var service = CreateService(out _, out var patientId, out _);

        var result = await service.CreateAsync(new CreateQueueTicketRequest(patientId, null, null, null));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.AppointmentId);
        Assert.Equal(QueueTicketStatus.Waiting, result.Value.Status);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenPatientMissing()
    {
        var service = CreateService(out _, out _, out _);

        var result = await service.CreateAsync(new CreateQueueTicketRequest(Guid.NewGuid(), null, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal("Queue.PatientNotFound", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenAppointmentMissing()
    {
        var service = CreateService(out _, out var patientId, out _);

        var result = await service.CreateAsync(new CreateQueueTicketRequest(patientId, Guid.NewGuid(), null, null));

        Assert.True(result.IsFailure);
        Assert.Equal("Appointment.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task StateMachine_ShouldFollow_WaitingCalledInProgressDone()
    {
        var service = CreateService(out _, out var patientId, out _);
        var ticket = await service.CreateAsync(new CreateQueueTicketRequest(patientId, null, null, null));
        var id = ticket.Value.Id;

        var called = await service.CallAsync(id);
        Assert.Equal(QueueTicketStatus.Called, called.Value.Status);
        Assert.NotNull(called.Value.CalledAt);

        var started = await service.StartAsync(id);
        Assert.Equal(QueueTicketStatus.InProgress, started.Value.Status);

        var done = await service.DoneAsync(id);
        Assert.Equal(QueueTicketStatus.Done, done.Value.Status);
    }

    [Fact]
    public async Task StartAsync_ShouldFail_WhenNotCalled()
    {
        var service = CreateService(out _, out var patientId, out _);
        var ticket = await service.CreateAsync(new CreateQueueTicketRequest(patientId, null, null, null));

        // Waiting → Start là chuyển tiếp sai.
        var result = await service.StartAsync(ticket.Value.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("Queue.InvalidTransition", result.Error.Code);
    }

    [Fact]
    public async Task SkipAsync_ShouldSucceed_FromWaiting()
    {
        var service = CreateService(out _, out var patientId, out _);
        var ticket = await service.CreateAsync(new CreateQueueTicketRequest(patientId, null, null, null));

        var result = await service.SkipAsync(ticket.Value.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(QueueTicketStatus.Skipped, result.Value.Status);
    }

    [Fact]
    public async Task GetListAsync_ShouldFilterByStatus()
    {
        var service = CreateService(out _, out var patientId, out _);
        var a = await service.CreateAsync(new CreateQueueTicketRequest(patientId, null, null, null));
        await service.CreateAsync(new CreateQueueTicketRequest(patientId, null, null, null));
        await service.CallAsync(a.Value.Id);

        var called = await service.GetListAsync(new QueueFilter(null, null, null, QueueTicketStatus.Called));
        var waiting = await service.GetListAsync(new QueueFilter(null, null, null, QueueTicketStatus.Waiting));

        Assert.Single(called.Value);
        Assert.Single(waiting.Value);
    }

    [Fact]
    public async Task AssignAsync_ShouldSetDoctor()
    {
        var service = CreateService(out var db, out var patientId, out _);
        var doctor = new ClinicManagement.Domain.Doctors.Doctor("BS-000001", "BS. C", Guid.NewGuid(), null, null);
        db.Doctors.Add(doctor);
        await db.SaveChangesAsync();
        var ticket = await service.CreateAsync(new CreateQueueTicketRequest(patientId, null, null, null));

        var result = await service.AssignAsync(ticket.Value.Id, new AssignQueueTicketRequest(null, doctor.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(doctor.Id, result.Value.DoctorId);
        Assert.Equal("BS. C", result.Value.DoctorName);
    }
}
