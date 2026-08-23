using ClinicManagement.Application.Appointments.Dtos;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Application.Visits.Dtos;
using ClinicManagement.Domain.Appointments;
using ClinicManagement.Domain.Billing;
using ClinicManagement.Domain.Paraclinical;
using ClinicManagement.Domain.Visits;
using ClinicManagement.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.Visits;

/// <summary>
/// Nghiệp vụ lượt tiếp đón: tạo một lượt kèm nhiều dịch vụ khám (mỗi dịch vụ = một
/// <see cref="Appointment"/> con), thêm dịch vụ vào lượt đang mở, và gom viện phí cả lượt
/// (suy từ hoá đơn gắn với các lịch trong lượt — không phi chuẩn hoá cột trên Invoice, ADR 0017).
/// </summary>
public sealed class VisitService : IVisitService
{
    private const int MaxPageSize = 100;
    private readonly IAppDbContext _db;

    public VisitService(IAppDbContext db) => _db = db;

    public async Task<Result<VisitDto>> CreateAsync(CreateVisitRequest request, CancellationToken ct = default)
    {
        var lines = request.Services ?? Array.Empty<VisitServiceLine>();
        var clsIds = (request.ParaclinicalServiceIds ?? Array.Empty<Guid>()).Distinct().ToList();
        if (lines.Count == 0 && clsIds.Count == 0)
            return Error.Validation("Visit.NoServices",
                "Lượt tiếp đón phải có ít nhất một dịch vụ (khám hoặc cận lâm sàng).");

        if (!await _db.Patients.AnyAsync(p => p.Id == request.PatientId, ct))
            return Error.Validation("Visit.PatientNotFound", $"Bệnh nhân với Id {request.PatientId} không tồn tại.");

        // Dựng cụm mục CLS trước (validate loại Paraclinical) để không tạo lượt khi CLS sai.
        List<LabOrderItem>? labItems = null;
        if (clsIds.Count > 0)
        {
            var built = await BuildLabItemsAsync(clsIds, ct);
            if (built.IsFailure)
                return Result.Failure<VisitDto>(built.Error);
            labItems = built.Value;
        }

        var code = await GenerateCodeAsync(ct);
        var visit = new Visit(code, request.PatientId, NormalizeOptional(request.Note));
        _db.Visits.Add(visit);

        // Kiểm & dựng từng dịch vụ khám. Chống trùng giờ xét cả lịch trong CSDL lẫn các lịch vừa thêm
        // trong cùng lượt (cùng bác sĩ) để không tạo hai dịch vụ chồng giờ cho một bác sĩ.
        var pending = new List<Appointment>();
        foreach (var line in lines)
        {
            var appt = await BuildAppointmentAsync(request.PatientId, visit.Id, line, pending, ct);
            if (appt.IsFailure)
                return Result.Failure<VisitDto>(appt.Error);

            pending.Add(appt.Value);
            _db.Appointments.Add(appt.Value);
        }

        // Gộp chỉ định CLS ngay lúc tiếp đón: một phiếu walk-in gắn lượt (ADR 0017).
        if (labItems is { Count: > 0 })
        {
            var labCode = await GenerateLabCodeAsync(ct);
            var order = LabOrder.CreateWalkIn(
                labCode, request.PatientId, appointmentId: null, note: null, labItems, visit.Id);
            _db.LabOrders.Add(order);
        }

        await _db.SaveChangesAsync(ct);
        return (await ProjectByIdAsync(visit.Id, ct))!;
    }

    /// <summary>Snapshot tên/giá cho các dịch vụ CLS; mọi dịch vụ phải tồn tại và thuộc loại Paraclinical.</summary>
    private async Task<Result<List<LabOrderItem>>> BuildLabItemsAsync(
        IReadOnlyList<Guid> serviceIds, CancellationToken ct)
    {
        var services = await _db.ServicePrices
            .Where(s => serviceIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, ct);

        var missing = serviceIds.Where(id => !services.ContainsKey(id)).ToList();
        if (missing.Count > 0)
            return Error.NotFound("ServicePrice.NotFound",
                $"Dịch vụ tham chiếu không tồn tại: {string.Join(", ", missing)}.");

        var notParaclinical = serviceIds.Where(id => services[id].Category != ServiceCategory.Paraclinical).ToList();
        if (notParaclinical.Count > 0)
            return Error.Validation("Paraclinical.ServiceNotParaclinical",
                $"Dịch vụ không thuộc loại cận lâm sàng: {string.Join(", ", notParaclinical)}.");

        return serviceIds.Select(id =>
        {
            var svc = services[id];
            return new LabOrderItem(svc.Id, svc.Name, svc.UnitPrice);
        }).ToList();
    }

    /// <summary>Sinh mã phiếu chỉ định CLS dạng CLS-000001, đếm cả bản ghi đã xoá mềm.</summary>
    private async Task<string> GenerateLabCodeAsync(CancellationToken ct)
    {
        var count = await _db.LabOrders.IgnoreQueryFilters().CountAsync(ct);
        return $"CLS-{count + 1:D6}";
    }

    public async Task<Result<VisitDto>> AddServiceAsync(
        Guid visitId, AddVisitServiceRequest request, CancellationToken ct = default)
    {
        var visit = await _db.Visits.FirstOrDefaultAsync(v => v.Id == visitId, ct);
        if (visit is null)
            return Error.NotFound("Visit.NotFound", $"Không tìm thấy lượt tiếp đón với Id {visitId}.");

        if (visit.Status != VisitStatus.Open)
            return Result.Failure<VisitDto>(Error.Conflict(
                "Visit.NotOpen", "Chỉ thêm dịch vụ được khi lượt còn đang mở (Open)."));

        var line = new VisitServiceLine(
            request.DoctorId, request.StartTime, request.EndTime, request.Reason, request.ServicePriceId);
        var appt = await BuildAppointmentAsync(visit.PatientId, visit.Id, line, Array.Empty<Appointment>(), ct);
        if (appt.IsFailure)
            return Result.Failure<VisitDto>(appt.Error);

        _db.Appointments.Add(appt.Value);
        await _db.SaveChangesAsync(ct);
        return (await ProjectByIdAsync(visit.Id, ct))!;
    }

    public async Task<Result<PagedResult<VisitListItemDto>>> GetListAsync(
        int page, int pageSize, Guid? patientId, VisitStatus? status, DateOnly? date, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > MaxPageSize ? 20 : pageSize;

        var query = _db.Visits.AsNoTracking();
        if (patientId is not null)
            query = query.Where(v => v.PatientId == patientId);
        if (status is not null)
            query = query.Where(v => v.Status == status);
        if (date is { } d)
        {
            var dayStart = new DateTimeOffset(d.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var dayEnd = dayStart.AddDays(1);
            query = query.Where(v => v.CreatedAt >= dayStart && v.CreatedAt < dayEnd);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => new VisitListItemDto(
                v.Id,
                v.Code,
                v.PatientId,
                _db.Patients.Where(p => p.Id == v.PatientId).Select(p => p.FullName).FirstOrDefault(),
                v.Status,
                v.Note,
                _db.Appointments.Count(a => a.VisitId == v.Id),
                v.CreatedAt))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<VisitListItemDto>(items, page, pageSize, total);
    }

    public async Task<Result<VisitDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await ProjectByIdAsync(id, ct);
        return dto is null
            ? Error.NotFound("Visit.NotFound", $"Không tìm thấy lượt tiếp đón với Id {id}.")
            : dto;
    }

    public Task<Result<VisitDto>> CloseAsync(Guid id, CancellationToken ct = default)
        => TransitionAsync(id, v => v.Close(), ct);

    public Task<Result<VisitDto>> CancelAsync(Guid id, CancellationToken ct = default)
        => TransitionAsync(id, v => v.Cancel(), ct);

    private async Task<Result<VisitDto>> TransitionAsync(
        Guid id, Func<Visit, Result> transition, CancellationToken ct)
    {
        var visit = await _db.Visits.FirstOrDefaultAsync(v => v.Id == id, ct);
        if (visit is null)
            return Error.NotFound("Visit.NotFound", $"Không tìm thấy lượt tiếp đón với Id {id}.");

        var result = transition(visit);
        if (result.IsFailure)
            return Result.Failure<VisitDto>(result.Error);

        await _db.SaveChangesAsync(ct);
        return (await ProjectByIdAsync(visit.Id, ct))!;
    }

    /// <summary>
    /// Kiểm hợp lệ một dòng dịch vụ (bác sĩ tồn tại, dịch vụ là loại Consultation, không trùng giờ bác sĩ)
    /// rồi dựng <see cref="Appointment"/> gắn lượt (chưa lưu). <paramref name="pendingSameVisit"/> là các lịch
    /// đã dựng trong cùng lượt (chưa lưu) để xét trùng giờ nội bộ lượt.
    /// </summary>
    private async Task<Result<Appointment>> BuildAppointmentAsync(
        Guid patientId, Guid visitId, VisitServiceLine line,
        IReadOnlyCollection<Appointment> pendingSameVisit, CancellationToken ct)
    {
        if (!await _db.Doctors.AnyAsync(d => d.Id == line.DoctorId, ct))
            return Error.Validation("Visit.DoctorNotFound", $"Bác sĩ với Id {line.DoctorId} không tồn tại.");

        var service = await ResolveServiceAsync(line.ServicePriceId, ct);
        if (service.IsFailure)
            return Result.Failure<Appointment>(service.Error);

        var overlapDb = await _db.Appointments.AnyAsync(a =>
            a.DoctorId == line.DoctorId &&
            Appointment.ActiveStatuses.Contains(a.Status) &&
            a.StartTime < line.EndTime && a.EndTime > line.StartTime, ct);
        var overlapPending = pendingSameVisit.Any(a =>
            a.DoctorId == line.DoctorId && a.StartTime < line.EndTime && a.EndTime > line.StartTime);
        if (overlapDb || overlapPending)
            return Error.Conflict("Appointment.Overlap", "Bác sĩ đã có lịch khác trùng khung giờ này.");

        return new Appointment(
            patientId,
            line.DoctorId,
            line.StartTime,
            line.EndTime,
            NormalizeOptional(line.Reason),
            service.Value?.Id,
            service.Value?.Name,
            service.Value?.UnitPrice,
            visitId);
    }

    /// <summary>Tra dịch vụ khám: phải tồn tại và thuộc loại Consultation. Null khi không gắn dịch vụ.</summary>
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

    /// <summary>Dựng <see cref="VisitDto"/>: nạp lịch trong lượt + tổng viện phí gom cả lượt.</summary>
    private async Task<VisitDto?> ProjectByIdAsync(Guid id, CancellationToken ct)
    {
        var visit = await _db.Visits.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id, ct);
        if (visit is null)
            return null;

        var patientName = await _db.Patients.AsNoTracking()
            .Where(p => p.Id == visit.PatientId).Select(p => p.FullName).FirstOrDefaultAsync(ct);

        var appointments = await _db.Appointments.AsNoTracking()
            .Where(a => a.VisitId == id)
            .OrderBy(a => a.StartTime)
            .Select(a => new AppointmentDto(
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
                a.CreatedAt,
                a.UpdatedAt))
            .ToListAsync(ct);

        // Viện phí gom cả lượt: hoá đơn gắn trực tiếp lượt (Invoice.VisitId, ADR 0017) hoặc gắn lịch
        // trong lượt (Invoice.AppointmentId — tương thích hoá đơn lập trước khi có cột VisitId).
        var apptIds = appointments.Select(a => a.Id).ToList();
        var invoices = await _db.Invoices.AsNoTracking()
            .Where(i => i.VisitId == id || (i.AppointmentId != null && apptIds.Contains(i.AppointmentId!.Value)))
            .Select(i => new InvoiceAmount(i.Status, i.TotalAmount))
            .ToListAsync(ct);

        var billed = invoices.Where(x => x.Status != InvoiceStatus.Cancelled).Sum(x => x.Total);
        var paid = invoices.Where(x => x.Status == InvoiceStatus.Paid).Sum(x => x.Total);

        // Phiếu CLS gắn lượt (walk-in gộp lúc tiếp đón, hoặc gắn sau).
        var labOrders = await _db.LabOrders.AsNoTracking()
            .Where(o => o.VisitId == id)
            .OrderBy(o => o.CreatedAt)
            .Select(o => new VisitLabOrderDto(
                o.Id, o.Code, o.Status, o.Items.Sum(i => i.UnitPrice), o.InvoicedAt, o.Items.Count))
            .ToListAsync(ct);

        return new VisitDto(
            visit.Id, visit.Code, visit.PatientId, patientName, visit.Status, visit.Note,
            appointments, labOrders, billed, paid, billed - paid, visit.CreatedAt, visit.UpdatedAt);
    }

    /// <summary>Sinh mã lượt dạng LK-000001, đếm cả bản ghi đã xoá mềm để tránh trùng mã.</summary>
    private async Task<string> GenerateCodeAsync(CancellationToken ct)
    {
        var count = await _db.Visits.IgnoreQueryFilters().CountAsync(ct);
        return $"LK-{count + 1:D6}";
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Cặp trạng thái + tổng tiền của một hoá đơn (để gộp viện phí cả lượt).</summary>
    private sealed record InvoiceAmount(InvoiceStatus Status, decimal Total);
}
