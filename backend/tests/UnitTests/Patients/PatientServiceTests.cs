using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Application.Patients;
using ClinicManagement.Application.Patients.Dtos;
using ClinicManagement.Domain.Patients;
using ClinicManagement.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace UnitTests.Patients;

public sealed class PatientServiceTests
{
    // DbContext tối giản cho test, dùng provider InMemory — không phụ thuộc Infrastructure/Npgsql.
    private sealed class TestDbContext : DbContext, IAppDbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<Patient> Patients => Set<Patient>();
    }

    private static PatientService CreateService(out TestDbContext db)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        db = new TestDbContext(options);
        return new PatientService(db);
    }

    private static CreatePatientRequest ValidRequest(string name = "Nguyễn Văn A") =>
        new(name, new DateOnly(1990, 1, 1), Gender.Male, "0912345678", "Hà Nội");

    [Fact]
    public async Task CreateAsync_ShouldSucceed_AndGenerateSequentialCode()
    {
        var service = CreateService(out _);

        var first = await service.CreateAsync(ValidRequest("A"));
        var second = await service.CreateAsync(ValidRequest("B"));

        Assert.True(first.IsSuccess);
        Assert.Equal("BN-000001", first.Value.Code);
        Assert.Equal("BN-000002", second.Value.Code);
    }

    [Fact]
    public async Task CreateAsync_ShouldTrimAndNormalizeOptionalFields()
    {
        var service = CreateService(out _);

        var result = await service.CreateAsync(
            new CreatePatientRequest("  Trần B  ", null, Gender.Female, "   ", null));

        Assert.True(result.IsSuccess);
        Assert.Equal("Trần B", result.Value.FullName);
        Assert.Null(result.Value.PhoneNumber); // chuỗi khoảng trắng -> null
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenMissing()
    {
        var service = CreateService(out _);

        var result = await service.GetByIdAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNotFound_WhenMissing()
    {
        var service = CreateService(out _);

        var result = await service.UpdateAsync(
            Guid.NewGuid(),
            new UpdatePatientRequest("X", null, Gender.Male, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyFields_WhenExists()
    {
        var service = CreateService(out _);
        var created = await service.CreateAsync(ValidRequest());

        var updated = await service.UpdateAsync(
            created.Value.Id,
            new UpdatePatientRequest("Nguyễn Văn An", null, Gender.Male, "0900000000", "TP.HCM"));

        Assert.True(updated.IsSuccess);
        Assert.Equal("Nguyễn Văn An", updated.Value.FullName);
        Assert.Equal("0900000000", updated.Value.PhoneNumber);
        Assert.Equal(created.Value.Code, updated.Value.Code); // mã không đổi
    }

    [Fact]
    public async Task GetListAsync_ShouldFilterBySearch_AndPage()
    {
        var service = CreateService(out _);
        await service.CreateAsync(ValidRequest("Nguyễn Văn A"));
        await service.CreateAsync(ValidRequest("Trần Thị B"));
        await service.CreateAsync(ValidRequest("Lê Văn C"));

        var result = await service.GetListAsync(page: 1, pageSize: 10, search: "trần");

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.Equal("Trần Thị B", result.Value.Items.Single().FullName);
    }

    [Fact]
    public async Task GetListAsync_ShouldClampInvalidPaging()
    {
        var service = CreateService(out _);
        await service.CreateAsync(ValidRequest());

        var result = await service.GetListAsync(page: 0, pageSize: 9999, search: null);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Page);      // page < 1 -> 1
        Assert.Equal(20, result.Value.PageSize);  // pageSize quá lớn -> mặc định 20
    }
}
