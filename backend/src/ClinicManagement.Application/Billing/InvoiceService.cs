using ClinicManagement.Application.Billing.Dtos;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Domain.Billing;
using ClinicManagement.Domain.Encounters;
using ClinicManagement.Domain.Paraclinical;
using ClinicManagement.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.Billing;

public sealed class InvoiceService : IInvoiceService
{
    private const int MaxPageSize = 100;
    private readonly IAppDbContext _db;

    public InvoiceService(IAppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Lập hoá đơn <b>thuốc</b> từ một phiếu khám đã hoàn tất (Mô hình A, ADR 0014 P2):
    /// chỉ gồm các dòng thuốc đã kê có gắn danh mục (công khám/CLS thu qua hoá đơn riêng lúc tiếp đón).
    /// Chống lập trùng bằng cờ <see cref="Encounter.MedicationInvoicedAt"/> (đã lập → 409).
    /// </summary>
    public async Task<Result<InvoiceDto>> CreateFromEncounterAsync(Guid encounterId, CancellationToken ct = default)
    {
        var encounter = await _db.Encounters.FirstOrDefaultAsync(e => e.Id == encounterId, ct);
        if (encounter is null)
            return Error.NotFound("Encounter.NotFound", $"Không tìm thấy phiếu khám với Id {encounterId}.");

        if (encounter.Status != EncounterStatus.Completed)
            return Result.Failure<InvoiceDto>(Error.Conflict(
                "Billing.EncounterNotCompleted", "Chỉ lập được hoá đơn từ phiếu khám đã hoàn tất."));

        // Chỉ dựng dòng thuốc: các dòng đơn có gắn danh mục (MedicationId), số lượng × giá bán (snapshot).
        var prescribed = encounter.PrescriptionItems
            .Where(p => p.MedicationId is not null)
            .ToList();
        if (prescribed.Count == 0)
            return Result.Failure<InvoiceDto>(Error.Validation(
                "Billing.NoMedicationToInvoice", "Phiếu khám không có dòng thuốc gắn danh mục để lập hoá đơn."));

        var medIds = prescribed.Select(p => p.MedicationId!.Value).Distinct().ToList();
        // Gồm cả thuốc đã xoá mềm (vẫn tính tiền theo giá đã lưu) — MedicationId không FK cứng (ADR 0011).
        var meds = await _db.Medications.IgnoreQueryFilters()
            .Where(m => medIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, ct);

        var missing = medIds.Where(id => !meds.ContainsKey(id)).ToList();
        if (missing.Count > 0)
            return Error.NotFound("Pharmacy.MedicationNotFound",
                $"Thuốc tham chiếu không tồn tại: {string.Join(", ", missing)}.");

        var items = prescribed.Select(p =>
        {
            var med = meds[p.MedicationId!.Value];
            return new InvoiceItem(InvoiceItemType.Medication, med.Name, med.SalePrice, p.Quantity, med.Id);
        }).ToList();

        // Cờ chống lập trùng (thay lá chắn unique EncounterId đã bỏ ở Mô hình A).
        var mark = encounter.MarkMedicationInvoiced(DateTimeOffset.UtcNow);
        if (mark.IsFailure)
            return Result.Failure<InvoiceDto>(mark.Error);

        var code = await GenerateCodeAsync(ct);
        var visitId = await ResolveVisitIdAsync(encounter.AppointmentId, ct);
        var invoice = new Invoice(code, encounter.PatientId, encounterId, note: null, items, encounter.AppointmentId, visitId);
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync(ct);

        return (await ProjectByIdAsync(invoice.Id, ct))!;
    }

    /// <summary>
    /// Lập hoá đơn <b>phí cận lâm sàng</b> từ một phiếu chỉ định (Mô hình A, ADR 0015): các dòng loại
    /// <see cref="InvoiceItemType.Paraclinical"/> snapshot theo giá đã chỉ định. Gắn lượt tiếp đón suy ra
    /// từ phiếu khám nguồn (nếu có). Chống lập trùng bằng cờ <see cref="LabOrder.InvoicedAt"/> (đã lập → 409).
    /// </summary>
    public async Task<Result<InvoiceDto>> CreateFromLabOrderAsync(Guid labOrderId, CancellationToken ct = default)
    {
        var order = await _db.LabOrders.FirstOrDefaultAsync(o => o.Id == labOrderId, ct);
        if (order is null)
            return Error.NotFound("Paraclinical.NotFound", $"Không tìm thấy phiếu chỉ định với Id {labOrderId}.");

        if (order.Status == LabOrderStatus.Cancelled)
            return Result.Failure<InvoiceDto>(Error.Conflict(
                "Billing.LabOrderCancelled", "Không lập được hoá đơn từ phiếu chỉ định đã huỷ."));

        if (order.Items.Count == 0)
            return Result.Failure<InvoiceDto>(Error.Validation(
                "Billing.NoItems", "Phiếu chỉ định không có dịch vụ để lập hoá đơn."));

        var items = order.Items
            .Select(i => new InvoiceItem(InvoiceItemType.Paraclinical, i.ServiceName, i.UnitPrice, 1, i.ServicePriceId))
            .ToList();

        // Cờ chống lập trùng (Mô hình A — không dựa lá chắn unique).
        var mark = order.MarkInvoiced(DateTimeOffset.UtcNow);
        if (mark.IsFailure)
            return Result.Failure<InvoiceDto>(mark.Error);

        // Suy ra lượt tiếp đón: ưu tiên lượt gắn trực tiếp trên phiếu chỉ định (walk-in, ADR 0016),
        // fallback từ phiếu khám nguồn (đường bác sĩ Sprint 15) — giữ nguyên hành vi cũ khi có encounter.
        var appointmentId = order.AppointmentId;
        if (appointmentId is null && order.EncounterId is not null)
        {
            appointmentId = await _db.Encounters.AsNoTracking()
                .Where(e => e.Id == order.EncounterId)
                .Select(e => (Guid?)e.AppointmentId)
                .FirstOrDefaultAsync(ct);
        }

        var code = await GenerateCodeAsync(ct);
        // Lượt: ưu tiên lượt gắn trực tiếp phiếu chỉ định (walk-in gắn lượt, ADR 0017), fallback suy từ lịch.
        var visitId = order.VisitId ?? await ResolveVisitIdAsync(appointmentId, ct);
        var invoice = new Invoice(code, order.PatientId, order.EncounterId, note: null, items, appointmentId, visitId,
            labOrderId: order.Id);
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync(ct);

        return (await ProjectByIdAsync(invoice.Id, ct))!;
    }

    public async Task<Result<InvoiceDto>> CreateAsync(CreateInvoiceRequest request, CancellationToken ct = default)
    {
        var lines = request.Items ?? Array.Empty<CreateInvoiceItemRequest>();
        if (lines.Count == 0)
            return Error.Validation("Billing.NoItems", "Hoá đơn phải có ít nhất một dòng.");

        if (!await _db.Patients.AnyAsync(p => p.Id == request.PatientId, ct))
            return Error.NotFound("Patient.NotFound", $"Không tìm thấy bệnh nhân với Id {request.PatientId}.");

        // Lượt tiếp đón (tuỳ chọn): kiểm tồn tại khi có, để null với vãng lai/chỉ-CLS.
        if (request.AppointmentId is not null &&
            !await _db.Appointments.AnyAsync(a => a.Id == request.AppointmentId, ct))
            return Error.NotFound("Appointment.NotFound",
                $"Không tìm thấy lượt khám với Id {request.AppointmentId}.");

        var build = await BuildServiceItemsAsync(lines, ct);
        if (build.IsFailure)
            return Result.Failure<InvoiceDto>(build.Error);

        var code = await GenerateCodeAsync(ct);
        var visitId = await ResolveVisitIdAsync(request.AppointmentId, ct);
        var invoice = new Invoice(code, request.PatientId, encounterId: null, NormalizeOptional(request.Note), build.Value,
            request.AppointmentId, visitId);
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync(ct);

        return (await ProjectByIdAsync(invoice.Id, ct))!;
    }

    public async Task<Result<PagedResult<InvoiceDto>>> GetListAsync(
        int page, int pageSize, Guid? patientId, Guid? appointmentId, InvoiceStatus? status,
        DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > MaxPageSize ? 20 : pageSize;

        var query = _db.Invoices.AsNoTracking();
        if (patientId is not null)
            query = query.Where(i => i.PatientId == patientId);
        if (appointmentId is not null)
            query = query.Where(i => i.AppointmentId == appointmentId);
        if (status is not null)
            query = query.Where(i => i.Status == status);
        if (from is not null)
            query = query.Where(i => i.CreatedAt >= from);
        if (to is not null)
            query = query.Where(i => i.CreatedAt <= to);

        var total = await query.CountAsync(ct);
        var items = await Project(query.OrderByDescending(i => i.CreatedAt))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<InvoiceDto>(items, page, pageSize, total);
    }

    public async Task<Result<InvoiceDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await ProjectByIdAsync(id, ct);
        return dto is null
            ? Error.NotFound("Invoice.NotFound", $"Không tìm thấy hoá đơn với Id {id}.")
            : dto;
    }

    public async Task<Result<AppointmentInvoicesDto>> GetByAppointmentAsync(
        Guid appointmentId, CancellationToken ct = default)
    {
        var invoices = await Project(
                _db.Invoices.AsNoTracking()
                    .Where(i => i.AppointmentId == appointmentId)
                    .OrderBy(i => i.CreatedAt))
            .ToListAsync(ct);

        // Đã huỷ không tính vào tổng đã lập; đã thu = các HĐ Paid; còn nợ = đã lập − đã thu.
        var billed = invoices.Where(i => i.Status != InvoiceStatus.Cancelled).Sum(i => i.TotalAmount);
        var paid = invoices.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.TotalAmount);

        return new AppointmentInvoicesDto(appointmentId, invoices, billed, paid, billed - paid);
    }

    public async Task<Result<VisitInvoicesDto>> GetByVisitAsync(Guid visitId, CancellationToken ct = default)
        => await BuildVisitInvoicesAsync(visitId, ct);

    public async Task<Result<VisitInvoicesDto>> PayVisitAsync(
        Guid visitId, PayInvoiceRequest request, CancellationToken ct = default)
    {
        // Thu toàn bộ hoá đơn còn Draft của lượt bằng một phương thức, trong một SaveChanges.
        var drafts = await _db.Invoices
            .Where(i => i.VisitId == visitId && i.Status == InvoiceStatus.Draft)
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;
        foreach (var inv in drafts)
        {
            inv.Pay(request.PaymentMethod, now);
            await MarkSourcesPaidAsync(inv, now, ct);
        }

        if (drafts.Count > 0)
            await _db.SaveChangesAsync(ct);

        return await BuildVisitInvoicesAsync(visitId, ct);
    }

    /// <summary>
    /// Móc "thu tiền → mở cổng thực hiện" (ADR 0021): khi một hoá đơn được thu, đánh dấu nguồn gắn nó đã thu —
    /// phiếu chỉ định CLS (<see cref="Invoice.LabOrderId"/> → <c>LabOrder.MarkPaid</c>, mở cổng nhập kết quả) và
    /// phiếu khám có dòng thuốc (<see cref="InvoiceItemType.Medication"/> → <c>Encounter.MarkMedicationPaid</c>,
    /// mở cổng cấp phát). Idempotent ở Domain; gọi trước <c>SaveChanges</c> để cùng transaction.
    /// </summary>
    private async Task MarkSourcesPaidAsync(Invoice invoice, DateTimeOffset when, CancellationToken ct)
    {
        if (invoice.LabOrderId is { } labOrderId)
        {
            var order = await _db.LabOrders.FirstOrDefaultAsync(o => o.Id == labOrderId, ct);
            order?.MarkPaid(when);
        }

        if (invoice.EncounterId is { } encounterId &&
            invoice.Items.Any(i => i.ItemType == InvoiceItemType.Medication))
        {
            var encounter = await _db.Encounters.FirstOrDefaultAsync(e => e.Id == encounterId, ct);
            encounter?.MarkMedicationPaid(when);
        }
    }

    private async Task<VisitInvoicesDto> BuildVisitInvoicesAsync(Guid visitId, CancellationToken ct)
    {
        var invoices = await Project(
                _db.Invoices.AsNoTracking()
                    .Where(i => i.VisitId == visitId)
                    .OrderBy(i => i.CreatedAt))
            .ToListAsync(ct);

        var billed = invoices.Where(i => i.Status != InvoiceStatus.Cancelled).Sum(i => i.TotalAmount);
        var paid = invoices.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.TotalAmount);

        return new VisitInvoicesDto(visitId, invoices, billed, paid, billed - paid);
    }

    /// <summary>Suy lượt tiếp đón từ lịch khám gắn hoá đơn (nếu lịch thuộc một lượt); null nếu không.</summary>
    private async Task<Guid?> ResolveVisitIdAsync(Guid? appointmentId, CancellationToken ct)
    {
        if (appointmentId is null)
            return null;

        return await _db.Appointments.AsNoTracking()
            .Where(a => a.Id == appointmentId)
            .Select(a => a.VisitId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Result<InvoiceDto>> UpdateAsync(
        Guid id, UpdateInvoiceRequest request, CancellationToken ct = default)
    {
        var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (invoice is null)
            return Error.NotFound("Invoice.NotFound", $"Không tìm thấy hoá đơn với Id {id}.");

        var lines = request.Items ?? Array.Empty<CreateInvoiceItemRequest>();
        if (lines.Count == 0)
            return Error.Validation("Billing.NoItems", "Hoá đơn phải có ít nhất một dòng.");

        var build = await BuildServiceItemsAsync(lines, ct);
        if (build.IsFailure)
            return Result.Failure<InvoiceDto>(build.Error);

        var replace = invoice.ReplaceItems(build.Value);
        if (replace.IsFailure)
            return Result.Failure<InvoiceDto>(replace.Error);

        invoice.UpdateNote(NormalizeOptional(request.Note));

        await _db.SaveChangesAsync(ct);
        return (await ProjectByIdAsync(invoice.Id, ct))!;
    }

    public async Task<Result<InvoiceDto>> PayAsync(
        Guid id, PayInvoiceRequest request, CancellationToken ct = default)
    {
        var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (invoice is null)
            return Error.NotFound("Invoice.NotFound", $"Không tìm thấy hoá đơn với Id {id}.");

        var now = DateTimeOffset.UtcNow;
        var pay = invoice.Pay(request.PaymentMethod, now);
        if (pay.IsFailure)
            return Result.Failure<InvoiceDto>(pay.Error);

        // Mở cổng thực hiện cho nguồn gắn hoá đơn (CLS/thuốc) — ADR 0021.
        await MarkSourcesPaidAsync(invoice, now, ct);

        await _db.SaveChangesAsync(ct);
        return (await ProjectByIdAsync(invoice.Id, ct))!;
    }

    public async Task<Result<InvoiceDto>> CancelAsync(Guid id, CancellationToken ct = default)
    {
        var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (invoice is null)
            return Error.NotFound("Invoice.NotFound", $"Không tìm thấy hoá đơn với Id {id}.");

        var cancel = invoice.Cancel();
        if (cancel.IsFailure)
            return Result.Failure<InvoiceDto>(cancel.Error);

        await _db.SaveChangesAsync(ct);
        return (await ProjectByIdAsync(invoice.Id, ct))!;
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (invoice is null)
            return Result.Failure(Error.NotFound("Invoice.NotFound", $"Không tìm thấy hoá đơn với Id {id}."));

        if (invoice.Status != InvoiceStatus.Draft)
            return Result.Failure(Error.Conflict(
                "Billing.InvalidTransition", "Chỉ xoá được hoá đơn khi còn ở trạng thái nháp (Draft)."));

        invoice.MarkAsDeleted();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <summary>Dựng danh sách dòng dịch vụ từ tham chiếu bảng giá (snapshot đơn giá). Dịch vụ thiếu → NotFound.</summary>
    private async Task<Result<List<InvoiceItem>>> BuildServiceItemsAsync(
        IReadOnlyList<CreateInvoiceItemRequest> lines, CancellationToken ct)
    {
        var serviceIds = lines.Select(l => l.ServicePriceId).Distinct().ToList();
        var services = await _db.ServicePrices
            .Where(s => serviceIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, ct);

        var missing = serviceIds.Where(id => !services.ContainsKey(id)).ToList();
        if (missing.Count > 0)
            return Error.NotFound("ServicePrice.NotFound",
                $"Dịch vụ tham chiếu không tồn tại: {string.Join(", ", missing)}.");

        return lines.Select(l =>
        {
            var svc = services[l.ServicePriceId];
            // Loại dòng suy ra từ phân loại dịch vụ: CLS → Paraclinical, còn lại → ServiceFee (ADR 0015).
            var itemType = svc.Category == ServiceCategory.Paraclinical
                ? InvoiceItemType.Paraclinical
                : InvoiceItemType.ServiceFee;
            return new InvoiceItem(itemType, svc.Name, svc.UnitPrice, l.Quantity, svc.Id);
        }).ToList();
    }

    /// <summary>Ánh xạ truy vấn Hoá đơn sang DTO kèm cụm dòng và tên bệnh nhân (subquery).</summary>
    private IQueryable<InvoiceDto> Project(IQueryable<Invoice> query) =>
        query.Select(i => new InvoiceDto(
            i.Id,
            i.Code,
            i.PatientId,
            _db.Patients.Where(p => p.Id == i.PatientId).Select(p => p.FullName).FirstOrDefault(),
            i.EncounterId,
            i.AppointmentId,
            i.VisitId,
            i.Status,
            i.TotalAmount,
            i.PaidAt,
            i.PaymentMethod,
            i.Note,
            i.Items.Select(it => new InvoiceItemDto(
                it.ItemType, it.Description, it.UnitPrice, it.Quantity, it.LineTotal, it.ReferenceId)).ToList(),
            i.CreatedAt,
            i.UpdatedAt));

    private async Task<InvoiceDto?> ProjectByIdAsync(Guid id, CancellationToken ct) =>
        await Project(_db.Invoices.AsNoTracking().Where(i => i.Id == id)).FirstOrDefaultAsync(ct);

    /// <summary>Sinh mã hoá đơn dạng HD-000001, đếm cả bản ghi đã xoá mềm để tránh trùng mã.</summary>
    private async Task<string> GenerateCodeAsync(CancellationToken ct)
    {
        var count = await _db.Invoices.IgnoreQueryFilters().CountAsync(ct);
        return $"HD-{count + 1:D6}";
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
