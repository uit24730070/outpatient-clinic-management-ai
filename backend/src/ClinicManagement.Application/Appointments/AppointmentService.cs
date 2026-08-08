using ClinicManagement.Application.Appointments.Dtos;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Domain.Appointments;
using ClinicManagement.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.Appointments;

public sealed class AppointmentService : IAppointmentService
{
    private const int MaxPageSize = 100;
    private readonly IAppDbContext _db;

    public AppointmentService(IAppDbContext db) => _db = db;

    public async Task<Result<AppointmentDto>> CreateAsync(
        CreateAppointmentRequest request, CancellationToken ct = default)
    {
        if (!await _db.Patients.AnyAsync(p => p.Id == request.PatientId, ct))
            return Error.Validation("Appointment.PatientNotFound",
                $"Bệnh nhân với Id {request.PatientId} không tồn tại.");

        if (!await _db.Doctors.AnyAsync(d => d.Id == request.DoctorId, ct))
            return Error.Validation("Appointment.DoctorNotFound",
                $"Bác sĩ với Id {request.DoctorId} không tồn tại.");

        if (await HasOverlapAsync(request.DoctorId, request.StartTime, request.EndTime, null, ct))
            return Error.Conflict("Appointment.Overlap",
                "Bác sĩ đã có lịch khác trùng khung giờ này.");

        var appointment = new Appointment(
            request.PatientId,
            request.DoctorId,
            request.StartTime,
            request.EndTime,
            NormalizeOptional(request.Reason));

        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync(ct);

        return (await ProjectByIdAsync(appointment.Id, ct))!;
    }

    public async Task<Result<PagedResult<AppointmentDto>>> GetListAsync(
        AppointmentFilter filter, CancellationToken ct = default)
    {
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize is < 1 or > MaxPageSize ? 20 : filter.PageSize;

        var query = _db.Appointments.AsNoTracking();

        if (filter.DoctorId is { } doctorId)
            query = query.Where(a => a.DoctorId == doctorId);

        if (filter.PatientId is { } patientId)
            query = query.Where(a => a.PatientId == patientId);

        if (filter.Status is { } status)
            query = query.Where(a => a.Status == status);

        if (filter.Date is { } date)
        {
            // Lọc theo ngày (UTC): [00:00, 24:00) của ngày đó.
            var dayStart = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var dayEnd = dayStart.AddDays(1);
            query = query.Where(a => a.StartTime >= dayStart && a.StartTime < dayEnd);
        }

        var total = await query.CountAsync(ct);
        var items = await Project(query.OrderBy(a => a.StartTime))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<AppointmentDto>(items, page, pageSize, total);
    }

    public async Task<Result<AppointmentDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await ProjectByIdAsync(id, ct);
        return dto is null
            ? Error.NotFound("Appointment.NotFound", $"Không tìm thấy lịch khám với Id {id}.")
            : dto;
    }

    public async Task<Result<AppointmentDto>> UpdateAsync(
        Guid id, UpdateAppointmentRequest request, CancellationToken ct = default)
    {
        var appointment = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (appointment is null)
            return Error.NotFound("Appointment.NotFound", $"Không tìm thấy lịch khám với Id {id}.");

        if (await HasOverlapAsync(appointment.DoctorId, request.StartTime, request.EndTime, id, ct))
            return Error.Conflict("Appointment.Overlap",
                "Bác sĩ đã có lịch khác trùng khung giờ này.");

        var reschedule = appointment.Reschedule(
            request.StartTime, request.EndTime, NormalizeOptional(request.Reason));
        if (reschedule.IsFailure)
            return Result.Failure<AppointmentDto>(reschedule.Error);

        await _db.SaveChangesAsync(ct);
        return (await ProjectByIdAsync(appointment.Id, ct))!;
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var appointment = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (appointment is null)
            return Result.Failure(Error.NotFound("Appointment.NotFound", $"Không tìm thấy lịch khám với Id {id}."));

        appointment.MarkAsDeleted();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public Task<Result<AppointmentDto>> CheckInAsync(Guid id, CancellationToken ct = default)
        => TransitionAsync(id, a => a.CheckIn(), ct);

    public Task<Result<AppointmentDto>> StartAsync(Guid id, CancellationToken ct = default)
        => TransitionAsync(id, a => a.Start(), ct);

    public Task<Result<AppointmentDto>> CompleteAsync(Guid id, CancellationToken ct = default)
        => TransitionAsync(id, a => a.Complete(), ct);

    public Task<Result<AppointmentDto>> CancelAsync(Guid id, CancellationToken ct = default)
        => TransitionAsync(id, a => a.Cancel(), ct);

    public Task<Result<AppointmentDto>> MarkNoShowAsync(Guid id, CancellationToken ct = default)
        => TransitionAsync(id, a => a.MarkNoShow(), ct);

    /// <summary>Nạp lịch, áp một chuyển trạng thái của Domain rồi lưu; lỗi chuyển tiếp giữ nguyên mã.</summary>
    private async Task<Result<AppointmentDto>> TransitionAsync(
        Guid id, Func<Appointment, Result> transition, CancellationToken ct)
    {
        var appointment = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (appointment is null)
            return Error.NotFound("Appointment.NotFound", $"Không tìm thấy lịch khám với Id {id}.");

        var result = transition(appointment);
        if (result.IsFailure)
            return Result.Failure<AppointmentDto>(result.Error);

        await _db.SaveChangesAsync(ct);
        return (await ProjectByIdAsync(appointment.Id, ct))!;
    }

    /// <summary>Có lịch khác của cùng bác sĩ (còn chiếm chỗ) chồng khung giờ hay không.</summary>
    private async Task<bool> HasOverlapAsync(
        Guid doctorId, DateTimeOffset start, DateTimeOffset end, Guid? excludeId, CancellationToken ct) =>
        await _db.Appointments.AnyAsync(a =>
            a.DoctorId == doctorId &&
            (excludeId == null || a.Id != excludeId) &&
            Appointment.ActiveStatuses.Contains(a.Status) &&
            a.StartTime < end && a.EndTime > start, ct);

    /// <summary>Ánh xạ truy vấn Lịch khám sang DTO kèm tên bệnh nhân & bác sĩ (subquery).</summary>
    private IQueryable<AppointmentDto> Project(IQueryable<Appointment> query) =>
        query.Select(a => new AppointmentDto(
            a.Id,
            a.PatientId,
            _db.Patients.Where(p => p.Id == a.PatientId).Select(p => p.FullName).FirstOrDefault(),
            a.DoctorId,
            _db.Doctors.Where(d => d.Id == a.DoctorId).Select(d => d.FullName).FirstOrDefault(),
            a.StartTime,
            a.EndTime,
            a.Reason,
            a.Status,
            a.CheckedInAt,
            a.CreatedAt,
            a.UpdatedAt));

    private async Task<AppointmentDto?> ProjectByIdAsync(Guid id, CancellationToken ct) =>
        await Project(_db.Appointments.AsNoTracking().Where(a => a.Id == id)).FirstOrDefaultAsync(ct);

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
