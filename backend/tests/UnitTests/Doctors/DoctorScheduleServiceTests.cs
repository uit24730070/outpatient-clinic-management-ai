using ClinicManagement.Application.Doctors;
using ClinicManagement.Application.Doctors.Dtos;
using ClinicManagement.Domain.Doctors;
using ClinicManagement.Shared.Results;
using UnitTests.Common;

namespace UnitTests.Doctors;

public sealed class DoctorScheduleServiceTests
{
    private static DoctorScheduleService CreateService(out TestDbContext db, out Guid doctorId)
    {
        db = TestDbContext.CreateInMemory();
        var doctor = new Doctor("BS-000001", "BS. Trần B", Guid.NewGuid(), null, null);
        db.Doctors.Add(doctor);
        db.SaveChanges();
        doctorId = doctor.Id;
        return new DoctorScheduleService(db);
    }

    private static CreateDoctorScheduleRequest Slot(
        DayOfWeek day, int startHour, int endHour, Guid? roomId = null) =>
        new(day, new TimeOnly(startHour, 0), new TimeOnly(endHour, 0), roomId);

    [Fact]
    public async Task CreateAsync_ShouldSucceed()
    {
        var service = CreateService(out _, out var doctorId);

        var result = await service.CreateAsync(doctorId, Slot(DayOfWeek.Monday, 8, 12));

        Assert.True(result.IsSuccess);
        Assert.Equal(DayOfWeek.Monday, result.Value.DayOfWeek);
        Assert.Equal(new TimeOnly(8, 0), result.Value.StartTime);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenDoctorMissing()
    {
        var service = CreateService(out _, out _);

        var result = await service.CreateAsync(Guid.NewGuid(), Slot(DayOfWeek.Monday, 8, 12));

        Assert.True(result.IsFailure);
        Assert.Equal("Doctor.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenOverlapSameDay()
    {
        var service = CreateService(out _, out var doctorId);
        await service.CreateAsync(doctorId, Slot(DayOfWeek.Monday, 8, 12));

        var overlap = await service.CreateAsync(doctorId, Slot(DayOfWeek.Monday, 11, 15));

        Assert.True(overlap.IsFailure);
        Assert.Equal(ErrorType.Conflict, overlap.Error.Type);
        Assert.Equal("Doctor.ScheduleOverlap", overlap.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_ShouldAllow_SameTimeDifferentDay()
    {
        var service = CreateService(out _, out var doctorId);
        await service.CreateAsync(doctorId, Slot(DayOfWeek.Monday, 8, 12));

        var other = await service.CreateAsync(doctorId, Slot(DayOfWeek.Tuesday, 8, 12));

        Assert.True(other.IsSuccess);
        var list = await service.GetByDoctorAsync(doctorId);
        Assert.Equal(2, list.Value.Count);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenRoomMissing()
    {
        var service = CreateService(out _, out var doctorId);

        var result = await service.CreateAsync(doctorId, Slot(DayOfWeek.Monday, 8, 12, Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal("Room.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveSlot()
    {
        var service = CreateService(out _, out var doctorId);
        var created = await service.CreateAsync(doctorId, Slot(DayOfWeek.Monday, 8, 12));

        var delete = await service.DeleteAsync(doctorId, created.Value.Id);
        Assert.True(delete.IsSuccess);

        var list = await service.GetByDoctorAsync(doctorId);
        Assert.Empty(list.Value);
    }
}
