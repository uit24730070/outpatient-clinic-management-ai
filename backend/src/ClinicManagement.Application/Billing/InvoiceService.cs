using ClinicManagement.Application.Billing.Dtos;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Domain.Billing;
using ClinicManagement.Domain.Encounters;
using ClinicManagement.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.Billing;

public sealed class InvoiceService : IInvoiceService
{
    private const int MaxPageSize = 100;
    private readonly IAppDbContext _db;
    private readonly BillingOptions _options;

    public InvoiceService(IAppDbContext db, BillingOptions options)
    {
        _db = db;
        _options = options;
    }

    public async Task<Result<InvoiceDto>> CreateFromEncounterAsync(Guid encounterId, CancellationToken ct = default)
    {
        var encounter = await _db.Encounters.FirstOrDefaultAsync(e => e.Id == encounterId, ct);
        if (encounter is null)
            return Error.NotFound("Encounter.NotFound", $"Không tìm thấy phiếu khám với Id {encounterId}.");

        if (encounter.Status != EncounterStatus.Completed)
            return Result.Failure<InvoiceDto>(Error.Conflict(
                "Billing.EncounterNotCompleted", "Chỉ lập được hoá đơn từ phiếu khám đã hoàn tất."));

        if (await _db.Invoices.AnyAsync(i => i.EncounterId == encounterId, ct))
            return Result.Failure<InvoiceDto>(Error.Conflict(
                "Billing.InvoiceAlreadyExists", "Phiếu khám này đã có hoá đơn."));

        var items = new List<InvoiceItem>();

        // Dòng công khám: lấy từ dịch vụ mặc định (cấu hình mã), snapshot đơn giá.
        var consultation = await _db.ServicePrices
            .FirstOrDefaultAsync(s => s.Code == _options.DefaultConsultationServiceCode, ct);
        if (consultation is null)
            return Error.NotFound("Billing.ConsultationServiceNotFound",
                $"Không tìm thấy dịch vụ công khám mặc định (mã {_options.DefaultConsultationServiceCode}).");
        items.Add(new InvoiceItem(
            InvoiceItemType.ServiceFee, consultation.Name, consultation.UnitPrice, 1, consultation.Id));

        // Dòng thuốc: các dòng đơn có gắn danh mục (MedicationId), số lượng × giá bán (snapshot).
        var prescribed = encounter.PrescriptionItems
            .Where(p => p.MedicationId is not null)
            .ToList();
        if (prescribed.Count > 0)
        {
            var medIds = prescribed.Select(p => p.MedicationId!.Value).Distinct().ToList();
            // Gồm cả thuốc đã xoá mềm (vẫn tính tiền theo giá đã lưu) — MedicationId không FK cứng (ADR 0011).
            var meds = await _db.Medications.IgnoreQueryFilters()
                .Where(m => medIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id, ct);

            var missing = medIds.Where(id => !meds.ContainsKey(id)).ToList();
            if (missing.Count > 0)
                return Error.NotFound("Pharmacy.MedicationNotFound",
                    $"Thuốc tham chiếu không tồn tại: {string.Join(", ", missing)}.");

            foreach (var p in prescribed)
            {
                var med = meds[p.MedicationId!.Value];
                items.Add(new InvoiceItem(
                    InvoiceItemType.Medication, med.Name, med.SalePrice, p.Quantity, med.Id));
            }
        }

        var code = await GenerateCodeAsync(ct);
        var invoice = new Invoice(code, encounter.PatientId, encounterId, note: null, items);
        _db.Invoices.Add(invoice);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Chạy đua tạo trùng: unique index EncounterId chặn ở DB.
            return Result.Failure<InvoiceDto>(Error.Conflict(
                "Billing.InvoiceAlreadyExists", "Phiếu khám này đã có hoá đơn."));
        }

        return (await ProjectByIdAsync(invoice.Id, ct))!;
    }

    public async Task<Result<InvoiceDto>> CreateAsync(CreateInvoiceRequest request, CancellationToken ct = default)
    {
        var lines = request.Items ?? Array.Empty<CreateInvoiceItemRequest>();
        if (lines.Count == 0)
            return Error.Validation("Billing.NoItems", "Hoá đơn phải có ít nhất một dòng.");

        if (!await _db.Patients.AnyAsync(p => p.Id == request.PatientId, ct))
            return Error.NotFound("Patient.NotFound", $"Không tìm thấy bệnh nhân với Id {request.PatientId}.");

        var build = await BuildServiceItemsAsync(lines, ct);
        if (build.IsFailure)
            return Result.Failure<InvoiceDto>(build.Error);

        var code = await GenerateCodeAsync(ct);
        var invoice = new Invoice(code, request.PatientId, encounterId: null, NormalizeOptional(request.Note), build.Value);
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync(ct);

        return (await ProjectByIdAsync(invoice.Id, ct))!;
    }

    public async Task<Result<PagedResult<InvoiceDto>>> GetListAsync(
        int page, int pageSize, Guid? patientId, InvoiceStatus? status,
        DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > MaxPageSize ? 20 : pageSize;

        var query = _db.Invoices.AsNoTracking();
        if (patientId is not null)
            query = query.Where(i => i.PatientId == patientId);
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

        var pay = invoice.Pay(request.PaymentMethod, DateTimeOffset.UtcNow);
        if (pay.IsFailure)
            return Result.Failure<InvoiceDto>(pay.Error);

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
            return new InvoiceItem(InvoiceItemType.ServiceFee, svc.Name, svc.UnitPrice, l.Quantity, svc.Id);
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
