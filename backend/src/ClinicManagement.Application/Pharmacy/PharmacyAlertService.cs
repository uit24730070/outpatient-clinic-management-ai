using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Application.Pharmacy.Dtos;
using ClinicManagement.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.Pharmacy;

public sealed class PharmacyAlertService : IPharmacyAlertService
{
    private const int MaxExpiringInDays = 365;
    private readonly IAppDbContext _db;

    public PharmacyAlertService(IAppDbContext db) => _db = db;

    public async Task<Result<PharmacyAlertsDto>> GetAlertsAsync(int expiringInDays, CancellationToken ct = default)
    {
        // Kẹp ngưỡng: 0..365 ngày (0 = chỉ liệt kê lô đã hết hạn).
        expiringInDays = expiringInDays < 0 ? 30 : Math.Min(expiringInDays, MaxExpiringInDays);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var threshold = today.AddDays(expiringInDays);

        // Tồn thấp: tồn tổng (Sum các lô chưa xoá, phía server) ≤ ngưỡng đặt lại.
        var lowStock = await _db.Medications.AsNoTracking()
            .Where(m => (_db.MedicationBatches.Where(b => b.MedicationId == m.Id)
                            .Sum(b => (int?)b.QuantityOnHand) ?? 0) <= m.ReorderLevel)
            .OrderBy(m => m.Name)
            .Select(m => new LowStockAlertDto(
                m.Id,
                m.Code,
                m.Name,
                m.Unit,
                _db.MedicationBatches.Where(b => b.MedicationId == m.Id).Sum(b => (int?)b.QuantityOnHand) ?? 0,
                m.ReorderLevel))
            .ToListAsync(ct);

        // Lô sắp/đã hết hạn: còn tồn > 0 và hạn dùng ≤ hôm nay + N (bao gồm lô đã quá hạn).
        var expiring = await _db.MedicationBatches.AsNoTracking()
            .Where(b => b.QuantityOnHand > 0 && b.ExpiryDate <= threshold)
            .OrderBy(b => b.ExpiryDate)
            .Select(b => new ExpiringBatchAlertDto(
                b.Id,
                b.MedicationId,
                _db.Medications.Where(m => m.Id == b.MedicationId).Select(m => m.Code).FirstOrDefault() ?? string.Empty,
                _db.Medications.Where(m => m.Id == b.MedicationId).Select(m => m.Name).FirstOrDefault() ?? string.Empty,
                b.BatchNumber,
                b.ExpiryDate,
                b.QuantityOnHand,
                b.ExpiryDate < today))
            .ToListAsync(ct);

        return new PharmacyAlertsDto(lowStock, expiring, expiringInDays);
    }
}
