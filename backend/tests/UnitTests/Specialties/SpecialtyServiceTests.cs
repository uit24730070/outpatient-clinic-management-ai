using ClinicManagement.Application.Specialties;
using ClinicManagement.Application.Specialties.Dtos;
using ClinicManagement.Shared.Results;
using UnitTests.Common;

namespace UnitTests.Specialties;

public sealed class SpecialtyServiceTests
{
    private static SpecialtyService CreateService(out TestDbContext db)
    {
        db = TestDbContext.CreateInMemory();
        return new SpecialtyService(db);
    }

    [Fact]
    public async Task CreateAsync_ShouldSucceed_AndTrimName()
    {
        var service = CreateService(out _);

        var result = await service.CreateAsync(new CreateSpecialtyRequest("  Tim mạch  ", "  Khoa tim  "));

        Assert.True(result.IsSuccess);
        Assert.Equal("Tim mạch", result.Value.Name);
        Assert.Equal("Khoa tim", result.Value.Description);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnConflict_WhenNameDuplicated_CaseInsensitive()
    {
        var service = CreateService(out _);
        await service.CreateAsync(new CreateSpecialtyRequest("Nội tổng quát", null));

        var dup = await service.CreateAsync(new CreateSpecialtyRequest("nội tổng quát", null));

        Assert.True(dup.IsFailure);
        Assert.Equal(ErrorType.Conflict, dup.Error.Type);
    }

    [Fact]
    public async Task UpdateAsync_ShouldAllowSameName_ForSameRecord()
    {
        var service = CreateService(out _);
        var created = await service.CreateAsync(new CreateSpecialtyRequest("Da liễu", null));

        var updated = await service.UpdateAsync(
            created.Value.Id, new UpdateSpecialtyRequest("Da liễu", "Cập nhật mô tả"));

        Assert.True(updated.IsSuccess);
        Assert.Equal("Cập nhật mô tả", updated.Value.Description);
    }

    [Fact]
    public async Task DeleteAsync_ShouldHideSpecialty_FromList()
    {
        var service = CreateService(out _);
        var created = await service.CreateAsync(new CreateSpecialtyRequest("Tai mũi họng", null));

        await service.DeleteAsync(created.Value.Id);

        var list = await service.GetListAsync(page: 1, pageSize: 10, search: null);
        Assert.Equal(0, list.Value.TotalCount);
    }
}
