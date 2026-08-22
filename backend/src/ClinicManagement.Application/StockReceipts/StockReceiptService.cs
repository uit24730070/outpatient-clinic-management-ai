using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Application.StockReceipts.Dtos;
using ClinicManagement.Domain.Pharmacy;
using ClinicManagement.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.StockReceipts;

public sealed class StockReceiptService : IStockReceiptService
{
    private const int MaxPageSize = 100;
    private readonly IAppDbContext _db;

    public StockReceiptService(IAppDbContext db) => _db = db;

    public async Task<Result<StockReceiptDto>> CreateAsync(
        CreateStockReceiptRequest request, CancellationToken ct = default)
    {
        var items = request.Items ?? Array.Empty<StockReceiptItemRequest>();
        if (items.Count == 0)
            return Error.Validation("StockReceipt.NoItems", "Phiếu nhập phải có ít nhất một dòng.");

        // Kiểm mọi thuốc trong phiếu đều tồn tại (chưa xoá) trước khi ghi.
        var medicationIds = items.Select(i => i.MedicationId).Distinct().ToList();
        var existingIds = await _db.Medications
            .Where(m => medicationIds.Contains(m.Id))
            .Select(m => m.Id)
            .ToListAsync(ct);
        var missing = medicationIds.Except(existingIds).ToList();
        if (missing.Count > 0)
            return Error.NotFound("Pharmacy.MedicationNotFound",
                $"Thuốc không tồn tại hoặc đã bị xoá: {string.Join(", ", missing)}.");

        var code = await GenerateCodeAsync(ct);
        var receipt = new StockReceipt(
            code,
            request.SupplierName.Trim(),
            request.ReceivedAt,
            NormalizeOptional(request.Note),
            items.Select(i => new StockReceiptItem(
                i.MedicationId, i.BatchNumber.Trim(), i.ExpiryDate, i.Quantity, i.UnitCost)));
        _db.StockReceipts.Add(receipt);

        var occurredAt = DateTimeOffset.UtcNow;
        // Gom lô theo (thuốc, số lô, hạn dùng): các dòng trùng khoá cộng dồn vào cùng một lô.
        var batchCache = new Dictionary<(Guid, string, DateOnly), MedicationBatch>();

        foreach (var item in items)
        {
            var key = (item.MedicationId, item.BatchNumber.Trim(), item.ExpiryDate);
            if (!batchCache.TryGetValue(key, out var batch))
            {
                batch = await _db.MedicationBatches.FirstOrDefaultAsync(b =>
                    b.MedicationId == key.Item1 &&
                    b.BatchNumber == key.Item2 &&
                    b.ExpiryDate == key.Item3, ct);

                if (batch is null)
                {
                    batch = new MedicationBatch(key.Item1, key.Item2, key.Item3, 0);
                    _db.MedicationBatches.Add(batch);
                }
                batchCache[key] = batch;
            }

            var increase = batch.Increase(item.Quantity);
            if (increase.IsFailure)
                return Result.Failure<StockReceiptDto>(increase.Error);

            _db.StockTransactions.Add(new StockTransaction(
                batch.Id,
                StockTransactionType.Import,
                item.Quantity,
                referenceType: nameof(StockReceipt),
                referenceId: receipt.Id,
                occurredAt: occurredAt));
        }

        await _db.SaveChangesAsync(ct);
        return (await ProjectByIdAsync(receipt.Id, ct))!;
    }

    public async Task<Result<PagedResult<StockReceiptDto>>> GetListAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > MaxPageSize ? 20 : pageSize;

        var query = _db.StockReceipts.AsNoTracking();
        var total = await query.CountAsync(ct);
        var items = await Project(query.OrderByDescending(r => r.ReceivedAt))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<StockReceiptDto>(items, page, pageSize, total);
    }

    public async Task<Result<StockReceiptDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await ProjectByIdAsync(id, ct);
        return dto is null
            ? Error.NotFound("StockReceipt.NotFound", $"Không tìm thấy phiếu nhập với Id {id}.")
            : dto;
    }

    /// <summary>Ánh xạ truy vấn Phiếu nhập sang DTO kèm cụm dòng và tên thuốc (subquery).</summary>
    private IQueryable<StockReceiptDto> Project(IQueryable<StockReceipt> query) =>
        query.Select(r => new StockReceiptDto(
            r.Id,
            r.Code,
            r.SupplierName,
            r.ReceivedAt,
            r.Note,
            r.Items.Select(i => new StockReceiptItemDto(
                i.MedicationId,
                _db.Medications.Where(m => m.Id == i.MedicationId).Select(m => m.Name).FirstOrDefault(),
                i.BatchNumber,
                i.ExpiryDate,
                i.Quantity,
                i.UnitCost)).ToList(),
            r.CreatedAt,
            r.UpdatedAt));

    private async Task<StockReceiptDto?> ProjectByIdAsync(Guid id, CancellationToken ct) =>
        await Project(_db.StockReceipts.AsNoTracking().Where(r => r.Id == id)).FirstOrDefaultAsync(ct);

    /// <summary>Sinh mã phiếu nhập dạng PN-000001, đếm cả bản ghi đã xoá mềm để tránh trùng mã.</summary>
    private async Task<string> GenerateCodeAsync(CancellationToken ct)
    {
        var count = await _db.StockReceipts.IgnoreQueryFilters().CountAsync(ct);
        return $"PN-{count + 1:D6}";
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
