using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Application.Doctors.Dtos;
using ClinicManagement.Domain.Doctors;
using ClinicManagement.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.Doctors;

public sealed class DoctorScheduleService : IDoctorScheduleService
{
    private readonly IAppDbContext _db;

    public DoctorScheduleService(IAppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<DoctorWorkScheduleDto>>> GetByDoctorAsync(
        Guid doctorId, CancellationToken ct = default)
    {
        if (!await _db.Doctors.AnyAsync(d => d.Id == doctorId, ct))
            return Error.NotFound("Doctor.NotFound", $"Không tìm thấy bác sĩ với Id {doctorId}.");

        var items = await Project(_db.DoctorWorkSchedules.AsNoTracking()
                .Where(s => s.DoctorId == doctorId)
                .OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime))
            .ToListAsync(ct);

        return items;
    }

    public async Task<Result<DoctorWorkScheduleDto>> CreateAsync(
        Guid doctorId, CreateDoctorScheduleRequest request, CancellationToken ct = default)
    {
        if (!await _db.Doctors.AnyAsync(d => d.Id == doctorId, ct))
            return Error.NotFound("Doctor.NotFound", $"Không tìm thấy bác sĩ với Id {doctorId}.");

        var roomCheck = await ValidateRoomAsync(request.RoomId, ct);
        if (roomCheck.IsFailure)
            return Result.Failure<DoctorWorkScheduleDto>(roomCheck.Error);

        if (await HasOverlapAsync(doctorId, request.DayOfWeek, request.StartTime, request.EndTime, null, ct))
            return Error.Conflict("Doctor.ScheduleOverlap",
                "Khung giờ làm việc bị chồng lấn với một khung đã có trong cùng thứ.");

        var schedule = new DoctorWorkSchedule(
            doctorId, request.DayOfWeek, request.StartTime, request.EndTime, request.RoomId);

        _db.DoctorWorkSchedules.Add(schedule);
        await _db.SaveChangesAsync(ct);

        return (await ProjectByIdAsync(schedule.Id, ct))!;
    }

    public async Task<Result<DoctorWorkScheduleDto>> UpdateAsync(
        Guid doctorId, Guid scheduleId, UpdateDoctorScheduleRequest request, CancellationToken ct = default)
    {
        var schedule = await _db.DoctorWorkSchedules
            .FirstOrDefaultAsync(s => s.Id == scheduleId && s.DoctorId == doctorId, ct);
        if (schedule is null)
            return Error.NotFound("Doctor.ScheduleNotFound",
                $"Không tìm thấy khung làm việc với Id {scheduleId} của bác sĩ này.");

        var roomCheck = await ValidateRoomAsync(request.RoomId, ct);
        if (roomCheck.IsFailure)
            return Result.Failure<DoctorWorkScheduleDto>(roomCheck.Error);

        if (await HasOverlapAsync(doctorId, request.DayOfWeek, request.StartTime, request.EndTime, scheduleId, ct))
            return Error.Conflict("Doctor.ScheduleOverlap",
                "Khung giờ làm việc bị chồng lấn với một khung đã có trong cùng thứ.");

        schedule.UpdateDetails(request.DayOfWeek, request.StartTime, request.EndTime, request.RoomId);
        await _db.SaveChangesAsync(ct);

        return (await ProjectByIdAsync(schedule.Id, ct))!;
    }

    public async Task<Result> DeleteAsync(Guid doctorId, Guid scheduleId, CancellationToken ct = default)
    {
        var schedule = await _db.DoctorWorkSchedules
            .FirstOrDefaultAsync(s => s.Id == scheduleId && s.DoctorId == doctorId, ct);
        if (schedule is null)
            return Result.Failure(Error.NotFound("Doctor.ScheduleNotFound",
                $"Không tìm thấy khung làm việc với Id {scheduleId} của bác sĩ này."));

        schedule.MarkAsDeleted();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task<Result> ValidateRoomAsync(Guid? roomId, CancellationToken ct)
    {
        if (roomId is null) return Result.Success();
        return await _db.Rooms.AnyAsync(r => r.Id == roomId, ct)
            ? Result.Success()
            : Result.Failure(Error.NotFound("Room.NotFound", $"Không tìm thấy phòng khám với Id {roomId}."));
    }

    /// <summary>Có khung khác cùng bác sĩ/cùng thứ chồng giờ hay không.</summary>
    private async Task<bool> HasOverlapAsync(
        Guid doctorId, DayOfWeek day, TimeOnly start, TimeOnly end, Guid? excludeId, CancellationToken ct) =>
        await _db.DoctorWorkSchedules.AnyAsync(s =>
            s.DoctorId == doctorId &&
            s.DayOfWeek == day &&
            (excludeId == null || s.Id != excludeId) &&
            s.StartTime < end && s.EndTime > start, ct);

    private IQueryable<DoctorWorkScheduleDto> Project(IQueryable<DoctorWorkSchedule> query) =>
        query.Select(s => new DoctorWorkScheduleDto(
            s.Id,
            s.DoctorId,
            s.DayOfWeek,
            s.StartTime,
            s.EndTime,
            s.RoomId,
            s.RoomId == null ? null : _db.Rooms.Where(r => r.Id == s.RoomId).Select(r => r.Name).FirstOrDefault(),
            s.CreatedAt,
            s.UpdatedAt));

    private async Task<DoctorWorkScheduleDto?> ProjectByIdAsync(Guid id, CancellationToken ct) =>
        await Project(_db.DoctorWorkSchedules.AsNoTracking().Where(s => s.Id == id)).FirstOrDefaultAsync(ct);
}
