using ClinicManagement.Application.Billing;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Application.Paraclinical.Dtos;
using ClinicManagement.Domain.Billing;
using ClinicManagement.Domain.Encounters;
using ClinicManagement.Domain.Paraclinical;
using ClinicManagement.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.Paraclinical;

public sealed class LabOrderService : ILabOrderService
{
    private const int MaxPageSize = 100;
    private readonly IAppDbContext _db;
    // Best-effort: tự lập hoá đơn CLS ngay khi bác sĩ chỉ định (null trong unit test → bỏ qua, dùng
    // lại nút "Lập HĐ CLS" thủ công ở LabOrderPanel/VisitDetailPage làm phương án dự phòng).
    private readonly IInvoiceService? _invoices;

    public LabOrderService(IAppDbContext db, IInvoiceService? invoices = null)
    {
        _db = db;
        _invoices = invoices;
    }

    public async Task<Result<LabOrderDto>> CreateFromEncounterAsync(
        CreateLabOrderRequest request, CancellationToken ct = default)
    {
        var lines = request.Items ?? Array.Empty<CreateLabOrderItemRequest>();
        if (lines.Count == 0)
            return Error.Validation("Paraclinical.NoItems", "Phiếu chỉ định phải có ít nhất một dịch vụ.");

        var encounter = await _db.Encounters.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.EncounterId, ct);
        if (encounter is null)
            return Error.NotFound("Encounter.NotFound", $"Không tìm thấy phiếu khám với Id {request.EncounterId}.");

        // Chỉ định trong lúc khám: phiếu khám còn ở trạng thái nháp (chưa chốt).
        if (encounter.Status != EncounterStatus.Draft)
            return Result.Failure<LabOrderDto>(Error.Conflict(
                "Paraclinical.EncounterNotDraft", "Chỉ chỉ định cận lâm sàng khi phiếu khám còn ở trạng thái nháp."));

        var itemsResult = await BuildItemsAsync(lines, ct);
        if (itemsResult.IsFailure)
            return Result.Failure<LabOrderDto>(itemsResult.Error);

        // Chống chỉ định trùng dịch vụ CLS trong cùng lượt tiếp nhận (đã đăng ký lúc tiếp nhận hoặc
        // do bác sĩ khác chỉ định) — tránh KTV làm lại xét nghiệm thừa + thu tiền 2 lần.
        var visitId = await _db.Appointments.AsNoTracking()
            .Where(a => a.Id == encounter.AppointmentId)
            .Select(a => a.VisitId)
            .FirstOrDefaultAsync(ct);
        if (visitId is not null)
        {
            var duplicateCheck = await CheckDuplicateInVisitAsync(visitId.Value, itemsResult.Value, ct);
            if (duplicateCheck.IsFailure)
                return Result.Failure<LabOrderDto>(duplicateCheck.Error);
        }

        var code = await GenerateCodeAsync(ct);
        var order = new LabOrder(
            code, encounter.Id, encounter.PatientId, encounter.DoctorId,
            NormalizeOptional(request.Note), itemsResult.Value, visitId);

        _db.LabOrders.Add(order);
        await _db.SaveChangesAsync(ct);

        await AutoInvoiceAsync(order.Id, ct);
        return (await ProjectByIdAsync(order.Id, ct))!;
    }

    /// <summary>
    /// Chặn chỉ định trùng dịch vụ CLS đã có trong cùng lượt tiếp nhận (phiếu nào cũng được — walk-in
    /// lễ tân, bác sĩ khác — miễn còn hiệu lực, chưa <see cref="LabOrderStatus.Cancelled"/>).
    /// </summary>
    private async Task<Result> CheckDuplicateInVisitAsync(
        Guid visitId, IReadOnlyList<LabOrderItem> items, CancellationToken ct)
    {
        var serviceIds = items.Select(i => i.ServicePriceId).ToList();
        var duplicateNames = await _db.LabOrders.AsNoTracking()
            .Where(o => o.VisitId == visitId && o.Status != LabOrderStatus.Cancelled)
            .SelectMany(o => o.Items)
            .Where(i => serviceIds.Contains(i.ServicePriceId))
            .Select(i => i.ServiceName)
            .Distinct()
            .ToListAsync(ct);

        return duplicateNames.Count == 0
            ? Result.Success()
            : Result.Failure(Error.Conflict("Paraclinical.AlreadyOrderedInVisit",
                $"Dịch vụ đã được chỉ định trong lượt tiếp nhận này: {string.Join(", ", duplicateNames)}."));
    }

    /// <summary>
    /// Tự lập hoá đơn CLS ngay khi chỉ định — thay cho việc chờ Lễ tân/Admin bấm "Lập HĐ CLS" thủ công.
    /// Best-effort: bỏ qua nếu <see cref="_invoices"/> là null (unit test) hoặc lập thất bại (không chặn
    /// việc tạo phiếu chỉ định — vẫn còn nút thủ công làm phương án dự phòng).
    /// </summary>
    private async Task AutoInvoiceAsync(Guid labOrderId, CancellationToken ct)
    {
        if (_invoices is null) return;
        await _invoices.CreateFromLabOrderAsync(labOrderId, ct);
    }

    public async Task<Result<LabOrderDto>> CreateWalkInAsync(
        CreateWalkInLabOrderRequest request, CancellationToken ct = default)
    {
        var lines = request.Items ?? Array.Empty<CreateLabOrderItemRequest>();
        if (lines.Count == 0)
            return Error.Validation("Paraclinical.NoItems", "Phiếu chỉ định phải có ít nhất một dịch vụ.");

        if (!await _db.Patients.AnyAsync(p => p.Id == request.PatientId, ct))
            return Error.NotFound("Patient.NotFound", $"Không tìm thấy bệnh nhân với Id {request.PatientId}.");

        // Lịch khám tuỳ chọn: kiểm tồn tại khi có (để null với bệnh nhân vãng lai chỉ làm CLS).
        if (request.AppointmentId is not null &&
            !await _db.Appointments.AnyAsync(a => a.Id == request.AppointmentId, ct))
            return Error.NotFound("Appointment.NotFound",
                $"Không tìm thấy lịch khám với Id {request.AppointmentId}.");

        // Lượt tiếp nhận tuỳ chọn (ADR 0017): kiểm tồn tại khi có, để gom phiếu CLS & hoá đơn theo lượt.
        if (request.VisitId is not null &&
            !await _db.Visits.AnyAsync(v => v.Id == request.VisitId, ct))
            return Error.NotFound("Visit.NotFound",
                $"Không tìm thấy lượt tiếp nhận với Id {request.VisitId}.");

        var itemsResult = await BuildItemsAsync(lines, ct);
        if (itemsResult.IsFailure)
            return Result.Failure<LabOrderDto>(itemsResult.Error);

        if (request.VisitId is { } walkInVisitId)
        {
            var duplicateCheck = await CheckDuplicateInVisitAsync(walkInVisitId, itemsResult.Value, ct);
            if (duplicateCheck.IsFailure)
                return Result.Failure<LabOrderDto>(duplicateCheck.Error);
        }

        var code = await GenerateCodeAsync(ct);
        var order = LabOrder.CreateWalkIn(
            code, request.PatientId, request.AppointmentId,
            NormalizeOptional(request.Note), itemsResult.Value, request.VisitId);

        _db.LabOrders.Add(order);
        await _db.SaveChangesAsync(ct);

        return (await ProjectByIdAsync(order.Id, ct))!;
    }

    /// <summary>Snapshot tên/giá dịch vụ cho các dòng chỉ định; mọi dịch vụ phải tồn tại và thuộc loại Paraclinical.</summary>
    private async Task<Result<List<LabOrderItem>>> BuildItemsAsync(
        IReadOnlyList<CreateLabOrderItemRequest> lines, CancellationToken ct)
    {
        var serviceIds = lines.Select(l => l.ServicePriceId).Distinct().ToList();
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

        return lines.Select(l =>
        {
            var svc = services[l.ServicePriceId];
            return new LabOrderItem(svc.Id, svc.Name, svc.UnitPrice);
        }).ToList();
    }

    public async Task<Result<PagedResult<LabOrderDto>>> GetListAsync(
        int page, int pageSize, Guid? encounterId, Guid? patientId, Guid? visitId, LabOrderStatus? status,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > MaxPageSize ? 20 : pageSize;

        var query = _db.LabOrders.AsNoTracking();
        if (encounterId is not null)
            query = query.Where(o => o.EncounterId == encounterId);
        if (patientId is not null)
            query = query.Where(o => o.PatientId == patientId);
        if (visitId is not null)
            query = query.Where(o => o.VisitId == visitId);
        if (status is not null)
            query = query.Where(o => o.Status == status);

        var total = await query.CountAsync(ct);
        var items = await Project(query.OrderByDescending(o => o.CreatedAt))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<LabOrderDto>(items, page, pageSize, total);
    }

    public async Task<Result<LabOrderDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await ProjectByIdAsync(id, ct);
        return dto is null
            ? Error.NotFound("Paraclinical.NotFound", $"Không tìm thấy phiếu chỉ định với Id {id}.")
            : dto;
    }

    public async Task<Result<LabOrderDto>> SetItemResultAsync(
        Guid id, Guid itemId, SetLabResultRequest request, CancellationToken ct = default)
    {
        var order = await _db.LabOrders.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null)
            return Error.NotFound("Paraclinical.NotFound", $"Không tìm thấy phiếu chỉ định với Id {id}.");

        // Gating thanh toán trước khi thực hiện (ADR 0021, PAY-01): chưa thu phí CLS → không cho nhập kết quả.
        if (order.PaidAt is null)
            return Result.Failure<LabOrderDto>(Error.Conflict("Paraclinical.NotPaid",
                "Phiếu chỉ định chưa được thanh toán phí cận lâm sàng; không thể nhập kết quả."));

        var result = order.SetItemResult(
            itemId, NormalizeOptional(request.ResultText), NormalizeOptional(request.Conclusion),
            DateTimeOffset.UtcNow);
        if (result.IsFailure)
            return Result.Failure<LabOrderDto>(result.Error);

        await _db.SaveChangesAsync(ct);
        return (await ProjectByIdAsync(order.Id, ct))!;
    }

    public async Task<Result<LabOrderDto>> CancelAsync(Guid id, CancellationToken ct = default)
    {
        var order = await _db.LabOrders.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null)
            return Error.NotFound("Paraclinical.NotFound", $"Không tìm thấy phiếu chỉ định với Id {id}.");

        // REF-03 (ADR 0022): cho phép huỷ phiếu đã thu tiền khi chưa có kết quả nào (Ordered).
        // Nếu đã nhập một phần kết quả (InProgress) → từ chối để tránh mất dữ liệu kết quả đã ghi.
        if (order.IsPaid && order.Status == LabOrderStatus.InProgress)
            return Result.Failure<LabOrderDto>(Error.Conflict(
                "Paraclinical.HasPartialResults",
                "Không thể huỷ phiếu chỉ định đã thu tiền khi đã nhập một phần kết quả."));

        var cancel = order.Cancel();
        if (cancel.IsFailure)
            return Result.Failure<LabOrderDto>(cancel.Error);

        // Khi đã thu tiền: tìm hoá đơn CLS liên quan (Paid) và hoàn tiền (REF-01/REF-03, ADR 0022).
        if (order.IsPaid)
        {
            var invoice = await _db.Invoices.FirstOrDefaultAsync(
                i => i.LabOrderId == order.Id && i.Status == InvoiceStatus.Paid, ct);
            invoice?.Refund("Huỷ phiếu chỉ định cận lâm sàng", DateTimeOffset.UtcNow);
        }

        await _db.SaveChangesAsync(ct);
        return (await ProjectByIdAsync(order.Id, ct))!;
    }

    /// <summary>Ánh xạ truy vấn phiếu chỉ định sang DTO kèm tên bệnh nhân/bác sĩ (subquery) và cụm mục.</summary>
    private IQueryable<LabOrderDto> Project(IQueryable<LabOrder> query) =>
        query.Select(o => new LabOrderDto(
            o.Id,
            o.Code,
            o.EncounterId,
            o.AppointmentId,
            o.VisitId,
            o.PatientId,
            _db.Patients.Where(p => p.Id == o.PatientId).Select(p => p.FullName).FirstOrDefault(),
            o.DoctorId,
            _db.Doctors.Where(d => d.Id == o.DoctorId).Select(d => d.FullName).FirstOrDefault(),
            o.Status,
            o.Note,
            o.TotalAmount,
            o.InvoicedAt,
            o.PaidAt,
            o.Items.Select(i => new LabOrderItemDto(
                i.Id, i.ServicePriceId, i.ServiceName, i.UnitPrice,
                i.ResultText, i.Conclusion, i.Status, i.ResultedAt)).ToList(),
            o.CreatedAt,
            o.UpdatedAt));

    private async Task<LabOrderDto?> ProjectByIdAsync(Guid id, CancellationToken ct) =>
        await Project(_db.LabOrders.AsNoTracking().Where(o => o.Id == id)).FirstOrDefaultAsync(ct);

    /// <summary>Sinh mã phiếu chỉ định dạng CLS-000001, đếm cả bản ghi đã xoá mềm để tránh trùng mã.</summary>
    private async Task<string> GenerateCodeAsync(CancellationToken ct)
    {
        var count = await _db.LabOrders.IgnoreQueryFilters().CountAsync(ct);
        return $"CLS-{count + 1:D6}";
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
