using ClinicManagement.Application.StockReceipts;
using ClinicManagement.Application.StockReceipts.Dtos;
using ClinicManagement.Domain.Pharmacy;
using ClinicManagement.Shared.Results;
using Microsoft.EntityFrameworkCore;
using UnitTests.Common;

namespace UnitTests.StockReceipts;

public sealed class StockReceiptServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 22, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Expiry = new(2027, 12, 31);

    private static StockReceiptService CreateServiceWithMedication(out TestDbContext db, out Guid medicationId)
    {
        db = TestDbContext.CreateInMemory();
        var medication = new Medication("TH-000001", "Paracetamol 500mg", "Paracetamol", "viên", 100, null);
        db.Medications.Add(medication);
        db.SaveChanges();
        medicationId = medication.Id;
        return new StockReceiptService(db);
    }

    private static CreateStockReceiptRequest Request(Guid medId, string batch, int qty, DateOnly? expiry = null) =>
        new("Công ty Dược ABC", Now, "Nhập đầu kỳ",
            new[] { new StockReceiptItemRequest(medId, batch, expiry ?? Expiry, qty, 1500m) });

    [Fact]
    public async Task CreateAsync_ShouldCreateBatch_IncreaseStock_WriteLedger()
    {
        var service = CreateServiceWithMedication(out var db, out var medId);

        var result = await service.CreateAsync(Request(medId, "L01", 30));

        Assert.True(result.IsSuccess);
        Assert.Equal("PN-000001", result.Value.Code);
        Assert.Single(result.Value.Items);
        Assert.Equal("Paracetamol 500mg", result.Value.Items[0].MedicationName);

        // Lô được tạo với tồn 30.
        var batch = await db.MedicationBatches.SingleAsync();
        Assert.Equal(30, batch.QuantityOnHand);
        Assert.Equal("L01", batch.BatchNumber);

        // Sổ cái ghi một giao dịch Import +30.
        var tx = await db.StockTransactions.SingleAsync();
        Assert.Equal(StockTransactionType.Import, tx.Type);
        Assert.Equal(30, tx.QuantityDelta);
        Assert.Equal(nameof(StockReceipt), tx.ReferenceType);
        Assert.Equal(result.Value.Id, tx.ReferenceId);
        Assert.Equal(batch.Id, tx.MedicationBatchId);
    }

    [Fact]
    public async Task CreateAsync_ShouldAccumulate_WhenSameBatchAcrossReceipts()
    {
        var service = CreateServiceWithMedication(out var db, out var medId);

        await service.CreateAsync(Request(medId, "L01", 30));
        await service.CreateAsync(Request(medId, "L01", 20));

        var batch = await db.MedicationBatches.SingleAsync();
        Assert.Equal(50, batch.QuantityOnHand); // cộng dồn cùng lô
        Assert.Equal(2, await db.StockTransactions.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_ShouldAccumulate_WhenDuplicateLinesInOneReceipt()
    {
        var service = CreateServiceWithMedication(out var db, out var medId);

        var request = new CreateStockReceiptRequest("NCC", Now, null, new[]
        {
            new StockReceiptItemRequest(medId, "L01", Expiry, 10, null),
            new StockReceiptItemRequest(medId, "L01", Expiry, 15, null),
        });

        var result = await service.CreateAsync(request);
        Assert.True(result.IsSuccess);

        var batch = await db.MedicationBatches.SingleAsync();
        Assert.Equal(25, batch.QuantityOnHand);
    }

    [Fact]
    public async Task CreateAsync_ShouldSeparateBatches_WhenDifferentExpiry()
    {
        var service = CreateServiceWithMedication(out var db, out var medId);

        var request = new CreateStockReceiptRequest("NCC", Now, null, new[]
        {
            new StockReceiptItemRequest(medId, "L01", new DateOnly(2027, 1, 1), 10, null),
            new StockReceiptItemRequest(medId, "L01", new DateOnly(2028, 1, 1), 15, null),
        });

        await service.CreateAsync(request);

        Assert.Equal(2, await db.MedicationBatches.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenMedicationMissing()
    {
        var service = CreateServiceWithMedication(out _, out _);

        var result = await service.CreateAsync(Request(Guid.NewGuid(), "L01", 10));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Pharmacy.MedicationNotFound", result.Error.Code);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnReceipt_OrNotFound()
    {
        var service = CreateServiceWithMedication(out _, out var medId);
        var created = await service.CreateAsync(Request(medId, "L01", 30));

        var found = await service.GetByIdAsync(created.Value.Id);
        Assert.True(found.IsSuccess);
        Assert.Equal("PN-000001", found.Value.Code);

        var missing = await service.GetByIdAsync(Guid.NewGuid());
        Assert.True(missing.IsFailure);
        Assert.Equal(ErrorType.NotFound, missing.Error.Type);
    }
}
