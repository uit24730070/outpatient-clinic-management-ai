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

    public LabOrderService(IAppDbContext db) => _db = db;

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

        // Snapshot tên/giá dịch vụ; mọi dịch vụ phải tồn tại và thuộc loại Paraclinical.
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

        var items = lines.Select(l =>
        {
            var svc = services[l.ServicePriceId];
            return new LabOrderItem(svc.Id, svc.Name, svc.UnitPrice);
        }).ToList();

        var code = await GenerateCodeAsync(ct);
        var order = new LabOrder(
            code, encounter.Id, encounter.PatientId, encounter.DoctorId,
            NormalizeOptional(request.Note), items);

        _db.LabOrders.Add(order);
        await _db.SaveChangesAsync(ct);

        return (await ProjectByIdAsync(order.Id, ct))!;
    }

    public async Task<Result<PagedResult<LabOrderDto>>> GetListAsync(
        int page, int pageSize, Guid? encounterId, Guid? patientId, LabOrderStatus? status,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > MaxPageSize ? 20 : pageSize;

        var query = _db.LabOrders.AsNoTracking();
        if (encounterId is not null)
            query = query.Where(o => o.EncounterId == encounterId);
        if (patientId is not null)
            query = query.Where(o => o.PatientId == patientId);
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

        var cancel = order.Cancel();
        if (cancel.IsFailure)
            return Result.Failure<LabOrderDto>(cancel.Error);

        await _db.SaveChangesAsync(ct);
        return (await ProjectByIdAsync(order.Id, ct))!;
    }

    /// <summary>Ánh xạ truy vấn phiếu chỉ định sang DTO kèm tên bệnh nhân/bác sĩ (subquery) và cụm mục.</summary>
    private IQueryable<LabOrderDto> Project(IQueryable<LabOrder> query) =>
        query.Select(o => new LabOrderDto(
            o.Id,
            o.Code,
            o.EncounterId,
            o.PatientId,
            _db.Patients.Where(p => p.Id == o.PatientId).Select(p => p.FullName).FirstOrDefault(),
            o.DoctorId,
            _db.Doctors.Where(d => d.Id == o.DoctorId).Select(d => d.FullName).FirstOrDefault(),
            o.Status,
            o.Note,
            o.TotalAmount,
            o.InvoicedAt,
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
