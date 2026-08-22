using ClinicManagement.Application.Billing;
using ClinicManagement.Application.Billing.Dtos;
using ClinicManagement.Domain.Billing;
using ClinicManagement.Shared.Results;
using UnitTests.Common;

namespace UnitTests.Billing;

public sealed class ServicePriceServiceTests
{
    private static ServicePriceService CreateService(out TestDbContext db)
    {
        db = TestDbContext.CreateInMemory();
        return new ServicePriceService(db);
    }

    private static CreateServicePriceRequest ValidRequest(string name = "Khám tổng quát", decimal price = 150000m) =>
        new(name, price, "Công khám bệnh thông thường");

    [Fact]
    public async Task CreateAsync_ShouldGenerateCode_AndKeepPrice()
    {
        var service = CreateService(out _);

        var result = await service.CreateAsync(ValidRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal("DV-000001", result.Value.Code);
        Assert.Equal(150000m, result.Value.UnitPrice);
    }

    [Fact]
    public async Task CreateAsync_ShouldIncrementCode_Sequentially()
    {
        var service = CreateService(out _);

        await service.CreateAsync(ValidRequest("Dịch vụ A"));
        var second = await service.CreateAsync(ValidRequest("Dịch vụ B"));

        Assert.Equal("DV-000002", second.Value.Code);
    }

    [Fact]
    public async Task CreateAsync_DefaultCategory_ShouldBeOther_ForBackCompat()
    {
        var service = CreateService(out _);

        // Không truyền Category → mặc định Other (tương thích dữ liệu Sprint 14).
        var result = await service.CreateAsync(new CreateServicePriceRequest("Khám", 100000m, null));

        Assert.True(result.IsSuccess);
        Assert.Equal(ServiceCategory.Other, result.Value.Category);
    }

    [Fact]
    public async Task GetListAsync_ShouldFilterByCategory()
    {
        var service = CreateService(out _);
        await service.CreateAsync(new CreateServicePriceRequest("Khám tổng quát", 150000m, null, ServiceCategory.Consultation));
        await service.CreateAsync(new CreateServicePriceRequest("Công thức máu", 80000m, null, ServiceCategory.Paraclinical));
        await service.CreateAsync(new CreateServicePriceRequest("X-quang ngực", 120000m, null, ServiceCategory.Paraclinical));

        var paraclinical = await service.GetListAsync(1, 20, null, ServiceCategory.Paraclinical);
        Assert.Equal(2, paraclinical.Value.TotalCount);

        var consultation = await service.GetListAsync(1, 20, null, ServiceCategory.Consultation);
        Assert.Equal(1, consultation.Value.TotalCount);
    }

    [Fact]
    public async Task GetListAsync_ShouldFilterBySearch_OnNameOrCode()
    {
        var service = CreateService(out _);
        await service.CreateAsync(ValidRequest("Khám tổng quát"));
        await service.CreateAsync(ValidRequest("Tái khám"));

        var byName = await service.GetListAsync(1, 20, "tái", null);
        Assert.Equal(1, byName.Value.TotalCount);

        var byCode = await service.GetListAsync(1, 20, "dv-000001", null);
        Assert.Equal(1, byCode.Value.TotalCount);
    }

    [Fact]
    public async Task UpdateAsync_ShouldChangeFields()
    {
        var service = CreateService(out _);
        var created = await service.CreateAsync(ValidRequest());

        var updated = await service.UpdateAsync(created.Value.Id,
            new UpdateServicePriceRequest("Khám VIP", 300000m, null));

        Assert.True(updated.IsSuccess);
        Assert.Equal("Khám VIP", updated.Value.Name);
        Assert.Equal(300000m, updated.Value.UnitPrice);
        Assert.Null(updated.Value.Description);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNotFound_WhenMissing()
    {
        var service = CreateService(out _);

        var result = await service.UpdateAsync(Guid.NewGuid(),
            new UpdateServicePriceRequest("X", 1000m, null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDelete_HideFromList_AndKeepCodeCounter()
    {
        var service = CreateService(out _);
        var created = await service.CreateAsync(ValidRequest());

        var delete = await service.DeleteAsync(created.Value.Id);
        Assert.True(delete.IsSuccess);

        var list = await service.GetListAsync(1, 20, null, null);
        Assert.Equal(0, list.Value.TotalCount);

        // Mã vẫn đếm bản ghi đã xoá để tránh trùng.
        var next = await service.CreateAsync(ValidRequest("Dịch vụ mới"));
        Assert.Equal("DV-000002", next.Value.Code);
    }
}
