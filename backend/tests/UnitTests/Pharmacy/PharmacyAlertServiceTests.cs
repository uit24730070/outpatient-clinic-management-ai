using ClinicManagement.Application.Pharmacy;
using ClinicManagement.Domain.Pharmacy;
using UnitTests.Common;

namespace UnitTests.Pharmacy;

/// <summary>Kiểm thử cảnh báo kho: tồn thấp và lô sắp/đã hết hạn (PH-08, ADR 0011).</summary>
public sealed class PharmacyAlertServiceTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static PharmacyAlertService Setup(out TestDbContext db)
    {
        db = TestDbContext.CreateInMemory();
        return new PharmacyAlertService(db);
    }

    private static Guid AddMed(TestDbContext db, string code, string name, int reorderLevel)
    {
        var med = new Medication(code, name, name, "viên", reorderLevel, null);
        db.Medications.Add(med);
        db.SaveChanges();
        return med.Id;
    }

    private static void AddBatch(TestDbContext db, Guid medId, DateOnly expiry, int qty)
    {
        db.MedicationBatches.Add(new MedicationBatch(medId, "L", expiry, qty));
        db.SaveChanges();
    }

    [Fact]
    public async Task GetAlerts_ShouldFlagLowStock_WhenAtOrBelowReorderLevel()
    {
        var service = Setup(out var db);
        var low = AddMed(db, "TH-000001", "Thuốc thấp", reorderLevel: 50);
        AddBatch(db, low, Today.AddYears(1), 30); // tồn 30 ≤ 50 → cảnh báo
        var ok = AddMed(db, "TH-000002", "Thuốc đủ", reorderLevel: 10);
        AddBatch(db, ok, Today.AddYears(1), 100); // tồn 100 > 10 → không cảnh báo

        var result = await service.GetAlertsAsync(30);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.LowStock);
        Assert.Equal(low, item.MedicationId);
        Assert.Equal(30, item.StockOnHand);
        Assert.Equal(50, item.ReorderLevel);
    }

    [Fact]
    public async Task GetAlerts_ShouldFlagExpiringAndExpired_ButNotFarFuture()
    {
        var service = Setup(out var db);
        var med = AddMed(db, "TH-000001", "Thuốc", reorderLevel: 0);
        AddBatch(db, med, Today.AddDays(-2), 5);   // đã hết hạn → cảnh báo (IsExpired)
        AddBatch(db, med, Today.AddDays(10), 5);   // sắp hết hạn trong 30 ngày → cảnh báo
        AddBatch(db, med, Today.AddDays(90), 5);   // còn xa → không cảnh báo

        var result = await service.GetAlertsAsync(30);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.ExpiringBatches.Count);
        Assert.Contains(result.Value.ExpiringBatches, b => b.IsExpired);
        Assert.Contains(result.Value.ExpiringBatches, b => !b.IsExpired);
    }

    [Fact]
    public async Task GetAlerts_ShouldIgnoreExpiringBatches_WithZeroStock()
    {
        var service = Setup(out var db);
        var med = AddMed(db, "TH-000001", "Thuốc", reorderLevel: 0);
        AddBatch(db, med, Today.AddDays(5), 0); // hết tồn → không cảnh báo hết hạn

        var result = await service.GetAlertsAsync(30);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.ExpiringBatches);
    }
}
