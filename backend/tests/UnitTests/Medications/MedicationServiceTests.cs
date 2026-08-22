using ClinicManagement.Application.Medications;
using ClinicManagement.Application.Medications.Dtos;
using ClinicManagement.Domain.Pharmacy;
using ClinicManagement.Shared.Results;
using UnitTests.Common;

namespace UnitTests.Medications;

public sealed class MedicationServiceTests
{
    private static MedicationService CreateService(out TestDbContext db)
    {
        db = TestDbContext.CreateInMemory();
        return new MedicationService(db);
    }

    private static CreateMedicationRequest ValidRequest(string name = "Paracetamol 500mg") =>
        new(name, "Paracetamol", "viên", 100, "Hạ sốt, giảm đau");

    [Fact]
    public async Task CreateAsync_ShouldGenerateCode_AndZeroStock()
    {
        var service = CreateService(out _);

        var result = await service.CreateAsync(ValidRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal("TH-000001", result.Value.Code);
        Assert.Equal("Paracetamol", result.Value.ActiveIngredient);
        Assert.Equal(0, result.Value.StockOnHand);
    }

    [Fact]
    public async Task CreateAsync_ShouldIncrementCode_Sequentially()
    {
        var service = CreateService(out _);

        await service.CreateAsync(ValidRequest("Thuốc A"));
        var second = await service.CreateAsync(ValidRequest("Thuốc B"));

        Assert.Equal("TH-000002", second.Value.Code);
    }

    [Fact]
    public async Task GetListAsync_ShouldFilterBySearch_OnNameOrIngredient()
    {
        var service = CreateService(out _);
        await service.CreateAsync(new CreateMedicationRequest("Amoxicillin 500mg", "Amoxicillin", "viên", 0, null));
        await service.CreateAsync(new CreateMedicationRequest("Paracetamol 500mg", "Paracetamol", "viên", 0, null));

        var byName = await service.GetListAsync(1, 20, "amox");
        Assert.Equal(1, byName.Value.TotalCount);

        var byIngredient = await service.GetListAsync(1, 20, "paracetamol");
        Assert.Equal(1, byIngredient.Value.TotalCount);
    }

    [Fact]
    public async Task UpdateAsync_ShouldChangeFields()
    {
        var service = CreateService(out _);
        var created = await service.CreateAsync(ValidRequest());

        var updated = await service.UpdateAsync(created.Value.Id,
            new UpdateMedicationRequest("Paracetamol 650mg", "Paracetamol", "viên", 50, null));

        Assert.True(updated.IsSuccess);
        Assert.Equal("Paracetamol 650mg", updated.Value.Name);
        Assert.Equal(50, updated.Value.ReorderLevel);
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDelete_HideFromList()
    {
        var service = CreateService(out _);
        var created = await service.CreateAsync(ValidRequest());

        var delete = await service.DeleteAsync(created.Value.Id);
        Assert.True(delete.IsSuccess);

        var list = await service.GetListAsync(1, 20, null);
        Assert.Equal(0, list.Value.TotalCount);

        // Mã vẫn đếm bản ghi đã xoá để tránh trùng.
        var next = await service.CreateAsync(ValidRequest("Thuốc mới"));
        Assert.Equal("TH-000002", next.Value.Code);
    }

    [Fact]
    public async Task StockOnHand_ShouldSumBatches_ExcludingDeleted()
    {
        var service = CreateService(out var db);
        var created = await service.CreateAsync(ValidRequest());
        var medId = created.Value.Id;

        db.MedicationBatches.Add(new MedicationBatch(medId, "L01", new DateOnly(2027, 1, 1), 30));
        db.MedicationBatches.Add(new MedicationBatch(medId, "L02", new DateOnly(2027, 6, 1), 20));
        var deleted = new MedicationBatch(medId, "L03", new DateOnly(2027, 12, 1), 100);
        deleted.MarkAsDeleted();
        db.MedicationBatches.Add(deleted);
        await db.SaveChangesAsync();

        var dto = await service.GetByIdAsync(medId);
        Assert.Equal(50, dto.Value.StockOnHand); // 30 + 20; lô đã xoá không tính

        var batches = await service.GetBatchesAsync(medId);
        Assert.Equal(2, batches.Value.Count);
        // Hạn gần nhất lên đầu.
        Assert.Equal("L01", batches.Value[0].BatchNumber);
    }

    [Fact]
    public async Task GetBatchesAsync_ShouldReturnNotFound_WhenMedicationMissing()
    {
        var service = CreateService(out _);

        var result = await service.GetBatchesAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
