using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Application.Queue.Dtos;
using ClinicManagement.Domain.Appointments;
using ClinicManagement.Domain.Queue;
using ClinicManagement.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.Queue;

public sealed class QueueService : IQueueService
{
    /// <summary>Múi giờ phòng khám (UTC+7) — dùng để chốt "hôm nay" khi cấp/lọc số (thống nhất ADR 0018).</summary>
    private static readonly TimeSpan ClinicOffset = TimeSpan.FromHours(7);

    private readonly IAppDbContext _db;

    public QueueService(IAppDbContext db) => _db = db;

    public async Task<Result<QueueTicketDto>> CreateAsync(
        CreateQueueTicketRequest request, CancellationToken ct = default)
    {
        if (!await _db.Patients.AnyAsync(p => p.Id == request.PatientId, ct))
            return Error.Validation("Queue.PatientNotFound",
                $"Bệnh nhân với Id {request.PatientId} không tồn tại.");

        if (request.AppointmentId is { } apptId &&
            !await _db.Appointments.AnyAsync(a => a.Id == apptId, ct))
            return Error.NotFound("Appointment.NotFound",
                $"Không tìm thấy lịch khám với Id {apptId}.");

        var roomCheck = await ValidateRoomAsync(request.RoomId, ct);
        if (roomCheck.IsFailure)
            return Result.Failure<QueueTicketDto>(roomCheck.Error);

        var doctorCheck = await ValidateDoctorAsync(request.DoctorId, ct);
        if (doctorCheck.IsFailure)
            return Result.Failure<QueueTicketDto>(doctorCheck.Error);

        var today = TodayLocal();
        var number = await NextNumberAsync(today, ct);

        var ticket = new QueueTicket(
            today, number, request.PatientId, request.AppointmentId, request.RoomId, request.DoctorId);

        _db.QueueTickets.Add(ticket);
        await _db.SaveChangesAsync(ct);

        return (await ProjectByIdAsync(ticket.Id, ct))!;
    }

    public async Task<Result<IReadOnlyList<QueueTicketDto>>> GetListAsync(
        QueueFilter filter, CancellationToken ct = default)
    {
        var date = filter.Date ?? TodayLocal();
        var query = _db.QueueTickets.AsNoTracking().Where(t => t.TicketDate == date);

        if (filter.RoomId is { } roomId)
            query = query.Where(t => t.RoomId == roomId);

        if (filter.DoctorId is { } doctorId)
            query = query.Where(t => t.DoctorId == doctorId);

        if (filter.Status is { } status)
            query = query.Where(t => t.Status == status);

        var items = await Project(query.OrderBy(t => t.Number)).ToListAsync(ct);
        return Result.Success<IReadOnlyList<QueueTicketDto>>(items);
    }

    public async Task<Result<QueueTicketDto>> AssignAsync(
        Guid id, AssignQueueTicketRequest request, CancellationToken ct = default)
    {
        var ticket = await _db.QueueTickets.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (ticket is null)
            return Error.NotFound("Queue.NotFound", $"Không tìm thấy vé hàng đợi với Id {id}.");

        var roomCheck = await ValidateRoomAsync(request.RoomId, ct);
        if (roomCheck.IsFailure)
            return Result.Failure<QueueTicketDto>(roomCheck.Error);

        var doctorCheck = await ValidateDoctorAsync(request.DoctorId, ct);
        if (doctorCheck.IsFailure)
            return Result.Failure<QueueTicketDto>(doctorCheck.Error);

        ticket.Assign(request.RoomId, request.DoctorId);
        await _db.SaveChangesAsync(ct);
        return (await ProjectByIdAsync(ticket.Id, ct))!;
    }

    /// <summary>
    /// Gọi số (Waiting → Called). Gộp luôn Check-in lịch khám gắn vé (Scheduled → CheckedIn, nếu có)
    /// để bác sĩ thấy ngay ở /my-clinic — khỏi phải thao tác Check-in thủ công riêng ở màn khác.
    /// </summary>
    public async Task<Result<QueueTicketDto>> CallAsync(Guid id, CancellationToken ct = default)
    {
        var ticket = await _db.QueueTickets.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (ticket is null)
            return Error.NotFound("Queue.NotFound", $"Không tìm thấy vé hàng đợi với Id {id}.");

        var result = ticket.Call();
        if (result.IsFailure)
            return Result.Failure<QueueTicketDto>(result.Error);

        if (ticket.AppointmentId is { } appointmentId)
        {
            var appointment = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == appointmentId, ct);
            if (appointment is { Status: AppointmentStatus.Scheduled })
                appointment.CheckIn();
        }

        await _db.SaveChangesAsync(ct);
        return (await ProjectByIdAsync(ticket.Id, ct))!;
    }

    public Task<Result<QueueTicketDto>> StartAsync(Guid id, CancellationToken ct = default)
        => TransitionAsync(id, t => t.Start(), ct);

    public Task<Result<QueueTicketDto>> DoneAsync(Guid id, CancellationToken ct = default)
        => TransitionAsync(id, t => t.Done(), ct);

    public Task<Result<QueueTicketDto>> SkipAsync(Guid id, CancellationToken ct = default)
        => TransitionAsync(id, t => t.Skip(), ct);

    private async Task<Result<QueueTicketDto>> TransitionAsync(
        Guid id, Func<QueueTicket, Result> transition, CancellationToken ct)
    {
        var ticket = await _db.QueueTickets.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (ticket is null)
            return Error.NotFound("Queue.NotFound", $"Không tìm thấy vé hàng đợi với Id {id}.");

        var result = transition(ticket);
        if (result.IsFailure)
            return Result.Failure<QueueTicketDto>(result.Error);

        await _db.SaveChangesAsync(ct);
        return (await ProjectByIdAsync(ticket.Id, ct))!;
    }

    /// <summary>Số kế tiếp trong ngày = số vé đã cấp hôm đó + 1 (đếm cả vé đã xoá mềm, tránh trùng — như mã BN-).</summary>
    private async Task<int> NextNumberAsync(DateOnly date, CancellationToken ct)
    {
        var count = await _db.QueueTickets.IgnoreQueryFilters()
            .CountAsync(t => t.TicketDate == date, ct);
        return count + 1;
    }

    private static DateOnly TodayLocal() =>
        DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(ClinicOffset).DateTime);

    private async Task<Result> ValidateRoomAsync(Guid? roomId, CancellationToken ct)
    {
        if (roomId is null) return Result.Success();
        return await _db.Rooms.AnyAsync(r => r.Id == roomId, ct)
            ? Result.Success()
            : Result.Failure(Error.NotFound("Room.NotFound", $"Không tìm thấy phòng khám với Id {roomId}."));
    }

    private async Task<Result> ValidateDoctorAsync(Guid? doctorId, CancellationToken ct)
    {
        if (doctorId is null) return Result.Success();
        return await _db.Doctors.AnyAsync(d => d.Id == doctorId, ct)
            ? Result.Success()
            : Result.Failure(Error.NotFound("Doctor.NotFound", $"Không tìm thấy bác sĩ với Id {doctorId}."));
    }

    private IQueryable<QueueTicketDto> Project(IQueryable<QueueTicket> query) =>
        query.Select(t => new QueueTicketDto(
            t.Id,
            t.TicketDate,
            t.Number,
            t.PatientId,
            _db.Patients.Where(p => p.Id == t.PatientId).Select(p => p.FullName).FirstOrDefault(),
            t.AppointmentId,
            t.RoomId,
            t.RoomId == null ? null : _db.Rooms.Where(r => r.Id == t.RoomId).Select(r => r.Name).FirstOrDefault(),
            t.DoctorId,
            t.DoctorId == null ? null : _db.Doctors.Where(d => d.Id == t.DoctorId).Select(d => d.FullName).FirstOrDefault(),
            t.Status,
            t.CalledAt,
            t.CreatedAt,
            t.UpdatedAt));

    private async Task<QueueTicketDto?> ProjectByIdAsync(Guid id, CancellationToken ct) =>
        await Project(_db.QueueTickets.AsNoTracking().Where(t => t.Id == id)).FirstOrDefaultAsync(ct);
}
