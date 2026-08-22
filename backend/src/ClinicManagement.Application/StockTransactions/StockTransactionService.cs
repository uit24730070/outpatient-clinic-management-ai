using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Application.StockTransactions.Dtos;
using ClinicManagement.Domain.Pharmacy;
using ClinicManagement.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.StockTransactions;

public sealed class StockTransactionService : IStockTransactionService
{
    private const int MaxPageSize = 100;
    private readonly IAppDbContext _db;

    public StockTransactionService(IAppDbContext db) => _db = db;

    public async Task<Result<PagedResult<StockTransactionDto>>> GetListAsync(
        int page, int pageSize, Guid? medicationId, StockTransactionType? type, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > MaxPageSize ? 20 : pageSize;

        var query = _db.StockTransactions.AsNoTracking();

        if (type is { } t)
            query = query.Where(x => x.Type == t);

        if (medicationId is { } medId)
        {
            // Lọc theo thuốc qua lô (batch → medicationId).
            var batchIds = _db.MedicationBatches.Where(b => b.MedicationId == medId).Select(b => b.Id);
            query = query.Where(x => batchIds.Contains(x.MedicationBatchId));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.OccurredAt)
            .Select(x => new StockTransactionDto(
                x.Id,
                x.MedicationBatchId,
                _db.MedicationBatches.Where(b => b.Id == x.MedicationBatchId)
                    .Select(b => b.MedicationId).FirstOrDefault(),
                _db.MedicationBatches.Where(b => b.Id == x.MedicationBatchId)
                    .Select(b => _db.Medications.Where(m => m.Id == b.MedicationId).Select(m => m.Name).FirstOrDefault())
                    .FirstOrDefault(),
                _db.MedicationBatches.Where(b => b.Id == x.MedicationBatchId)
                    .Select(b => b.BatchNumber).FirstOrDefault(),
                x.Type,
                x.QuantityDelta,
                x.ReferenceType,
                x.ReferenceId,
                x.OccurredAt))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<StockTransactionDto>(items, page, pageSize, total);
    }
}
