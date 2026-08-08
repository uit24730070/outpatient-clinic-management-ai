using ClinicManagement.Application.Appointments;
using ClinicManagement.Application.Appointments.Dtos;
using ClinicManagement.Domain.Appointments;
using ClinicManagement.Domain.Doctors;
using ClinicManagement.Domain.Patients;
using ClinicManagement.Shared.Results;
using UnitTests.Common;

namespace UnitTests.Appointments;

public sealed class AppointmentServiceTests
{
    private static readonly DateTimeOffset Base =
        new(2026, 8, 10, 8, 0, 0, TimeSpan.Zero);

    private static AppointmentService CreateService(out TestDbContext db, out Guid patientId, out Guid doctorId)
    {
        db = TestDbContext.CreateInMemory();

        var patient = new Patient("BN-000001", "Nguyễn Văn A", null, Gender.Male, null, null);
        var doctor = new Doctor("BS-000001", "BS. Trần B", Guid.NewGuid(), null, null);
        db.Patients.Add(patient);
        db.Doctors.Add(doctor);
        db.SaveChanges();

        patientId = patient.Id;
        doctorId = doctor.Id;
        return new AppointmentService(db);
    }

    private static CreateAppointmentRequest ValidRequest(
        Guid patientId, Guid doctorId, DateTimeOffset? start = null, DateTimeOffset? end = null) =>
        new(patientId, doctorId, start ?? Base, end ?? Base.AddMinutes(30), "Khám tổng quát");

    [Fact]
    public async Task CreateAsync_ShouldSucceed_WithScheduledStatus()
    {
        var service = CreateService(out _, out var patientId, out var doctorId);

        var result = await service.CreateAsync(ValidRequest(patientId, doctorId));

        Assert.True(result.IsSuccess);
        Assert.Equal(AppointmentStatus.Scheduled, result.Value.Status);
        Assert.Equal("Nguyễn Văn A", result.Value.PatientName);
        Assert.Equal("BS. Trần B", result.Value.DoctorName);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenPatientMissing()
    {
        var service = CreateService(out _, out _, out var doctorId);

        var result = await service.CreateAsync(ValidRequest(Guid.NewGuid(), doctorId));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Appointment.PatientNotFound", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenDoctorMissing()
    {
        var service = CreateService(out _, out var patientId, out _);

        var result = await service.CreateAsync(ValidRequest(patientId, Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal("Appointment.DoctorNotFound", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenDoctorSlotOverlaps()
    {
        var service = CreateService(out _, out var patientId, out var doctorId);
        await service.CreateAsync(ValidRequest(patientId, doctorId, Base, Base.AddMinutes(30)));

        // Chồng 15 phút với lịch trên.
        var overlap = await service.CreateAsync(
            ValidRequest(patientId, doctorId, Base.AddMinutes(15), Base.AddMinutes(45)));

        Assert.True(overlap.IsFailure);
        Assert.Equal(ErrorType.Conflict, overlap.Error.Type);
        Assert.Equal("Appointment.Overlap", overlap.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_ShouldSucceed_WhenSlotsAreAdjacent()
    {
        var service = CreateService(out _, out var patientId, out var doctorId);
        await service.CreateAsync(ValidRequest(patientId, doctorId, Base, Base.AddMinutes(30)));

        // Bắt đầu đúng lúc lịch trước kết thúc → không chồng.
        var adjacent = await service.CreateAsync(
            ValidRequest(patientId, doctorId, Base.AddMinutes(30), Base.AddMinutes(60)));

        Assert.True(adjacent.IsSuccess);
    }

    [Fact]
    public async Task CreateAsync_ShouldAllowOverlap_WhenExistingIsCancelled()
    {
        var service = CreateService(out _, out var patientId, out var doctorId);
        var first = await service.CreateAsync(ValidRequest(patientId, doctorId));
        await service.CancelAsync(first.Value.Id);

        // Lịch đã huỷ không còn chiếm chỗ.
        var second = await service.CreateAsync(ValidRequest(patientId, doctorId));

        Assert.True(second.IsSuccess);
    }

    [Fact]
    public async Task CheckIn_ThenStart_ThenComplete_ShouldFollowStateMachine()
    {
        var service = CreateService(out _, out var patientId, out var doctorId);
        var created = await service.CreateAsync(ValidRequest(patientId, doctorId));
        var id = created.Value.Id;

        var checkedIn = await service.CheckInAsync(id);
        Assert.True(checkedIn.IsSuccess);
        Assert.Equal(AppointmentStatus.CheckedIn, checkedIn.Value.Status);
        Assert.NotNull(checkedIn.Value.CheckedInAt);

        var started = await service.StartAsync(id);
        Assert.Equal(AppointmentStatus.InProgress, started.Value.Status);

        var completed = await service.CompleteAsync(id);
        Assert.Equal(AppointmentStatus.Completed, completed.Value.Status);
    }

    [Fact]
    public async Task Start_ShouldFail_WhenNotCheckedIn()
    {
        var service = CreateService(out _, out var patientId, out var doctorId);
        var created = await service.CreateAsync(ValidRequest(patientId, doctorId));

        // Scheduled → Start là chuyển tiếp không hợp lệ.
        var started = await service.StartAsync(created.Value.Id);

        Assert.True(started.IsFailure);
        Assert.Equal(ErrorType.Conflict, started.Error.Type);
        Assert.Equal("Appointment.InvalidTransition", started.Error.Code);
    }

    [Fact]
    public async Task Complete_ShouldFail_AfterCancel()
    {
        var service = CreateService(out _, out var patientId, out var doctorId);
        var created = await service.CreateAsync(ValidRequest(patientId, doctorId));
        await service.CancelAsync(created.Value.Id);

        var completed = await service.CompleteAsync(created.Value.Id);

        Assert.True(completed.IsFailure);
        Assert.Equal("Appointment.InvalidTransition", completed.Error.Code);
    }

    [Fact]
    public async Task CheckIn_ShouldReturnNotFound_WhenMissing()
    {
        var service = CreateService(out _, out _, out _);

        var result = await service.CheckInAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task GetListAsync_ShouldFilterByDoctorAndStatus()
    {
        var service = CreateService(out var db, out var patientId, out var doctorId);

        var otherDoctor = new Doctor("BS-000002", "BS. Lê C", Guid.NewGuid(), null, null);
        db.Doctors.Add(otherDoctor);
        await db.SaveChangesAsync();

        await service.CreateAsync(ValidRequest(patientId, doctorId, Base, Base.AddMinutes(30)));
        await service.CreateAsync(ValidRequest(patientId, otherDoctor.Id, Base, Base.AddMinutes(30)));

        var byDoctor = await service.GetListAsync(new AppointmentFilter(DoctorId: doctorId));
        Assert.Equal(1, byDoctor.Value.TotalCount);

        var byStatus = await service.GetListAsync(new AppointmentFilter(Status: AppointmentStatus.Scheduled));
        Assert.Equal(2, byStatus.Value.TotalCount);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReschedule_WhenScheduled()
    {
        var service = CreateService(out _, out var patientId, out var doctorId);
        var created = await service.CreateAsync(ValidRequest(patientId, doctorId));

        var updated = await service.UpdateAsync(
            created.Value.Id,
            new UpdateAppointmentRequest(Base.AddHours(2), Base.AddHours(2).AddMinutes(30), "Đổi giờ"));

        Assert.True(updated.IsSuccess);
        Assert.Equal(Base.AddHours(2), updated.Value.StartTime);
        Assert.Equal("Đổi giờ", updated.Value.Reason);
    }

    [Fact]
    public async Task DeleteAsync_ShouldHideAppointment()
    {
        var service = CreateService(out _, out var patientId, out var doctorId);
        var created = await service.CreateAsync(ValidRequest(patientId, doctorId));

        var deleted = await service.DeleteAsync(created.Value.Id);
        Assert.True(deleted.IsSuccess);

        var getById = await service.GetByIdAsync(created.Value.Id);
        Assert.True(getById.IsFailure);
        Assert.Equal(ErrorType.NotFound, getById.Error.Type);
    }
}
