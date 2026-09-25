using ClinicManagement.Application.Appointments.Dtos;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Domain.Appointments;
using ClinicManagement.Domain.Billing;
using ClinicManagement.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.Appointments;

public sealed class AppointmentService : IAppointmentService
{
    private const int MaxPageSize = 100;

    /// <summary>
    /// Múi giờ phòng khám (Việt Nam, UTC+7, không có DST). Khung làm việc bác sĩ lưu giờ địa phương
    /// (<c>TimeOnly</c>); lịch khám lưu <c>timestamptz</c> UTC — quy đổi về giờ này khi so khung (ADR 0018).
    /// </summary>
    private static readonly TimeSpan ClinicOffset = TimeSpan.FromHours(7);

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

        var withinHours = await IsWithinWorkingHoursAsync(
            request.DoctorId, request.StartTime, request.EndTime, ct);
        if (withinHours.IsFailure)
            return Result.Failure<AppointmentDto>(withinHours.Error);

        var roomCheck = await ValidateRoomAsync(request.RoomId, ct);
        if (roomCheck.IsFailure)
            return Result.Failure<AppointmentDto>(roomCheck.Error);

        var service = await ResolveServiceAsync(request.ServicePriceId, ct);
        if (service.IsFailure)
            return Result.Failure<AppointmentDto>(service.Error);

        var appointment = new Appointment(
            request.PatientId,
            request.DoctorId,
            request.StartTime,
            request.EndTime,
            NormalizeOptional(request.Reason),
            service.Value?.Id,
            service.Value?.Name,
            service.Value?.UnitPrice,
            roomId: request.RoomId);

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
            // Ngày `date` là ngày theo giờ phòng khám (FE gửi lên từ ngày local trình duyệt) — quy
            // đổi mốc 00:00 giờ phòng khám về UTC (offset=0) để so đúng với cột StartTime.
            var dayStart = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), ClinicOffset).ToUniversalTime();
            var dayEnd = dayStart.AddDays(1);
            query = query.Where(a => a.StartTime >= dayStart && a.StartTime < dayEnd);
        }

        var total = await query.CountAsync(ct);
        var items = await Project(ApplySort(query, filter.SortBy, filter.SortDesc))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<AppointmentDto>(items, page, pageSize, total);
    }

    /// <summary>Sắp xếp theo cột do FE chọn (danh sách trắng); mặc định StartTime asc (giữ hành vi cũ).</summary>
    private static IOrderedQueryable<Appointment> ApplySort(IQueryable<Appointment> query, string? sortBy, bool desc) =>
        sortBy switch
        {
            "startTime" when desc => query.OrderByDescending(a => a.StartTime),
            "status" => desc ? query.OrderByDescending(a => a.Status) : query.OrderBy(a => a.Status),
            _ => query.OrderBy(a => a.StartTime),
        };

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

        var withinHours = await IsWithinWorkingHoursAsync(
            appointment.DoctorId, request.StartTime, request.EndTime, ct);
        if (withinHours.IsFailure)
            return Result.Failure<AppointmentDto>(withinHours.Error);

        var roomCheck = await ValidateRoomAsync(request.RoomId, ct);
        if (roomCheck.IsFailure)
            return Result.Failure<AppointmentDto>(roomCheck.Error);

        var service = await ResolveServiceAsync(request.ServicePriceId, ct);
        if (service.IsFailure)
            return Result.Failure<AppointmentDto>(service.Error);

        var reschedule = appointment.Reschedule(
            request.StartTime, request.EndTime, NormalizeOptional(request.Reason));
        if (reschedule.IsFailure)
            return Result.Failure<AppointmentDto>(reschedule.Error);

        appointment.SetService(service.Value?.Id, service.Value?.Name, service.Value?.UnitPrice);
        appointment.SetRoom(request.RoomId);

        await _db.SaveChangesAsync(ct);
        return (await ProjectByIdAsync(appointment.Id, ct))!;
    }

    public async Task<Result<AppointmentDto?>> GetLastForPatientAsync(Guid patientId, CancellationToken ct = default)
    {
        var dto = await Project(_db.Appointments.AsNoTracking()
                .Where(a => a.PatientId == patientId)
                .OrderByDescending(a => a.StartTime))
            .FirstOrDefaultAsync(ct);
        return Result.Success<AppointmentDto?>(dto);
    }

    /// <summary>
    /// Tra dịch vụ khám theo Id (nếu có): phải tồn tại và thuộc loại <see cref="ServiceCategory.Consultation"/>.
    /// Trả null khi không gắn dịch vụ (tương thích lịch không dịch vụ).
    /// </summary>
    private async Task<Result<ServicePrice?>> ResolveServiceAsync(Guid? servicePriceId, CancellationToken ct)
    {
        if (servicePriceId is null)
            return Result.Success<ServicePrice?>(null);

        var service = await _db.ServicePrices.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == servicePriceId, ct);
        if (service is null)
            return Error.NotFound("ServicePrice.NotFound",
                $"Dịch vụ khám với Id {servicePriceId} không tồn tại.");

        if (service.Category != ServiceCategory.Consultation)
            return Error.Validation("Appointment.ServiceNotConsultation",
                "Dịch vụ đăng ký khi đặt lịch phải thuộc loại công khám (Consultation).");

        return Result.Success<ServicePrice?>(service);
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

    // Bắt đầu khám đòi hỏi đã có sinh hiệu (điều dưỡng/admin đo) — khép kín quy trình, bác sĩ không
    // tự bỏ qua bước điều dưỡng được nữa (trước Sprint 25 vai trò này gần như tuỳ chọn).
    public Task<Result<AppointmentDto>> StartAsync(Guid id, CancellationToken ct = default)
        => TransitionAsync(id, a => a.Start(), ct, EnsureVitalsRecordedAsync);

    /// <summary>
    /// Đã có ít nhất một lần đo sinh hiệu cho lịch khám (gom theo Lượt nếu lịch thuộc một Lượt tiếp
    /// nhận — cùng cách gom của <c>VitalsService</c>) chưa. Chặn "Bắt đầu khám" nếu chưa có.
    /// </summary>
    private async Task<Result> EnsureVitalsRecordedAsync(Appointment appointment, CancellationToken ct)
    {
        var hasVitals = appointment.VisitId is { } visitId
            ? await _db.Vitals.AsNoTracking().AnyAsync(v => v.VisitId == visitId, ct)
            : await _db.Vitals.AsNoTracking().AnyAsync(v => v.AppointmentId == appointment.Id, ct);

        return hasVitals
            ? Result.Success()
            : Result.Failure(Error.Conflict("Appointment.VitalsRequired",
                "Cần đo sinh hiệu trước khi bắt đầu khám — nhờ điều dưỡng đo tại màn \"Sinh hiệu & Hàng đợi\"."));
    }

    public Task<Result<AppointmentDto>> CompleteAsync(Guid id, CancellationToken ct = default)
        => TransitionAsync(id, a => a.Complete(), ct);

    public Task<Result<AppointmentDto>> CancelAsync(Guid id, CancellationToken ct = default)
        => TransitionAsync(id, a => a.Cancel(), ct);

    public Task<Result<AppointmentDto>> MarkNoShowAsync(Guid id, CancellationToken ct = default)
        => TransitionAsync(id, a => a.MarkNoShow(), ct);

    /// <summary>
    /// Nạp lịch, áp một chuyển trạng thái của Domain, rồi (tuỳ chọn) kiểm điều kiện tiên quyết ngoài
    /// máy trạng thái trước khi lưu — máy trạng thái báo lỗi trước (vd Scheduled → Start vẫn phải là
    /// <c>InvalidTransition</c>, không bị át bởi điều kiện tiên quyết) vì chưa <c>SaveChangesAsync</c>
    /// nên chuyển trạng thái thử không lưu xuống DB nếu bước sau thất bại.
    /// </summary>
    private async Task<Result<AppointmentDto>> TransitionAsync(
        Guid id, Func<Appointment, Result> transition, CancellationToken ct,
        Func<Appointment, CancellationToken, Task<Result>>? preCheck = null)
    {
        var appointment = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (appointment is null)
            return Error.NotFound("Appointment.NotFound", $"Không tìm thấy lịch khám với Id {id}.");

        var result = transition(appointment);
        if (result.IsFailure)
            return Result.Failure<AppointmentDto>(result.Error);

        if (preCheck is not null)
        {
            var preCheckResult = await preCheck(appointment, ct);
            if (preCheckResult.IsFailure)
                return Result.Failure<AppointmentDto>(preCheckResult.Error);
        }

        await _db.SaveChangesAsync(ct);
        return (await ProjectByIdAsync(appointment.Id, ct))!;
    }

    /// <summary>
    /// Kiểm giờ khám nằm trong <b>một khung làm việc</b> của bác sĩ hôm đó (ADR 0018). Quy đổi thời gian
    /// UTC của lịch về giờ địa phương phòng khám (<see cref="ClinicOffset"/>) rồi so với <c>TimeOnly</c>.
    /// Bác sĩ <b>chưa khai lịch làm việc nào</b> ⇒ không ràng buộc (tương thích lịch cũ). Nếu đã khai mà
    /// giờ khám không lọt khung nào ⇒ <c>Appointment.OutsideWorkingHours</c> (409).
    /// </summary>
    private async Task<Result> IsWithinWorkingHoursAsync(
        Guid doctorId, DateTimeOffset start, DateTimeOffset end, CancellationToken ct)
    {
        var schedules = await _db.DoctorWorkSchedules.AsNoTracking()
            .Where(s => s.DoctorId == doctorId)
            .ToListAsync(ct);

        // Chưa khai lịch làm việc → cho đặt tự do.
        if (schedules.Count == 0)
            return Result.Success();

        var localStart = start.ToOffset(ClinicOffset);
        var localEnd = end.ToOffset(ClinicOffset);
        var startTime = TimeOnly.FromTimeSpan(localStart.TimeOfDay);
        var endTime = TimeOnly.FromTimeSpan(localEnd.TimeOfDay);

        // Khung khám không được vắt qua nửa đêm (giữ đơn giản; cùng ngày địa phương).
        var fits = localStart.Date == localEnd.Date && schedules.Any(s =>
            s.DayOfWeek == localStart.DayOfWeek &&
            s.StartTime <= startTime && s.EndTime >= endTime);

        return fits
            ? Result.Success()
            : Result.Failure(Error.Conflict("Appointment.OutsideWorkingHours",
                "Giờ khám nằm ngoài khung giờ làm việc của bác sĩ."));
    }

    private async Task<Result> ValidateRoomAsync(Guid? roomId, CancellationToken ct)
    {
        if (roomId is null) return Result.Success();
        return await _db.Rooms.AnyAsync(r => r.Id == roomId, ct)
            ? Result.Success()
            : Result.Failure(Error.NotFound("Room.NotFound", $"Không tìm thấy phòng khám với Id {roomId}."));
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
            a.ServicePriceId,
            a.ServiceName,
            a.ServicePrice,
            a.RoomId,
            a.RoomId == null ? null : _db.Rooms.Where(r => r.Id == a.RoomId).Select(r => r.Name).FirstOrDefault(),
            a.CreatedAt,
            a.UpdatedAt,
            a.InvoicedAt,
            a.VisitId,
            _db.QueueTickets.Where(t => t.AppointmentId == a.Id).Select(t => (int?)t.Number).FirstOrDefault()));

    private async Task<AppointmentDto?> ProjectByIdAsync(Guid id, CancellationToken ct) =>
        await Project(_db.Appointments.AsNoTracking().Where(a => a.Id == id)).FirstOrDefaultAsync(ct);

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
