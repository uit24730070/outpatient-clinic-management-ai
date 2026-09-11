using ClinicManagement.Application.Appointments;
using ClinicManagement.Application.Appointments.Dtos;
using ClinicManagement.Domain.Appointments;
using ClinicManagement.Domain.Doctors;
using ClinicManagement.Domain.Patients;
using ClinicManagement.Domain.Resources;
using ClinicManagement.Shared.Results;
using UnitTests.Common;

namespace UnitTests.Appointments;

/// <summary>
/// Kiểm ràng buộc giờ làm việc (WS-03) + gán phòng của <see cref="AppointmentService"/>.
/// Múi giờ phòng khám = UTC+7 → lịch UTC 03:00 tương ứng giờ địa phương 10:00.
/// </summary>
public sealed class AppointmentWorkingHoursTests
{
    // UTC 03:00 → local (UTC+7) = 10:00.
    private static readonly DateTimeOffset StartUtc = new(2026, 8, 10, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset EndUtc = StartUtc.AddMinutes(30);
    private static DayOfWeek LocalDay => StartUtc.ToOffset(TimeSpan.FromHours(7)).DayOfWeek;

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

    private static void AddSchedule(TestDbContext db, Guid doctorId, int startHour, int endHour)
    {
        db.DoctorWorkSchedules.Add(new DoctorWorkSchedule(
            doctorId, LocalDay, new TimeOnly(startHour, 0), new TimeOnly(endHour, 0), null));
        db.SaveChanges();
    }

    private static CreateAppointmentRequest Request(Guid patientId, Guid doctorId, Guid? roomId = null) =>
        new(patientId, doctorId, StartUtc, EndUtc, "Khám", RoomId: roomId);

    [Fact]
    public async Task CreateAsync_ShouldSucceed_WhenNoScheduleDeclared()
    {
        // Bác sĩ chưa khai lịch làm việc → cho đặt tự do (tương thích lịch cũ).
        var service = CreateService(out _, out var patientId, out var doctorId);

        var result = await service.CreateAsync(Request(patientId, doctorId));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CreateAsync_ShouldSucceed_WhenWithinWorkingHours()
    {
        var service = CreateService(out var db, out var patientId, out var doctorId);
        AddSchedule(db, doctorId, 8, 17); // 10:00 nằm trong 08:00–17:00

        var result = await service.CreateAsync(Request(patientId, doctorId));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenOutsideWorkingHours()
    {
        var service = CreateService(out var db, out var patientId, out var doctorId);
        AddSchedule(db, doctorId, 8, 9); // 10:00 ngoài 08:00–09:00

        var result = await service.CreateAsync(Request(patientId, doctorId));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Appointment.OutsideWorkingHours", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_ShouldAssignRoom_WhenRoomProvided()
    {
        var service = CreateService(out var db, out var patientId, out var doctorId);
        var room = new Room("PK-000001", "Phòng 101", null);
        db.Rooms.Add(room);
        db.SaveChanges();

        var result = await service.CreateAsync(Request(patientId, doctorId, room.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(room.Id, result.Value.RoomId);
        Assert.Equal("Phòng 101", result.Value.RoomName);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenRoomMissing()
    {
        var service = CreateService(out _, out var patientId, out var doctorId);

        var result = await service.CreateAsync(Request(patientId, doctorId, Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal("Room.NotFound", result.Error.Code);
    }
}
