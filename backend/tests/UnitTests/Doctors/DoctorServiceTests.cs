using ClinicManagement.Application.Doctors;
using ClinicManagement.Application.Doctors.Dtos;
using ClinicManagement.Application.Specialties;
using ClinicManagement.Application.Specialties.Dtos;
using ClinicManagement.Shared.Results;
using UnitTests.Common;

namespace UnitTests.Doctors;

public sealed class DoctorServiceTests
{
    private static (DoctorService doctors, SpecialtyService specialties) CreateServices(out TestDbContext db)
    {
        db = TestDbContext.CreateInMemory();
        return (new DoctorService(db), new SpecialtyService(db));
    }

    private static async Task<Guid> SeedSpecialtyAsync(SpecialtyService specialties, string name = "Nội tổng quát")
    {
        var created = await specialties.CreateAsync(new CreateSpecialtyRequest(name, null));
        return created.Value.Id;
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenSpecialtyNotFound()
    {
        var (doctors, _) = CreateServices(out _);

        var result = await doctors.CreateAsync(
            new CreateDoctorRequest("BS Nguyễn Văn A", Guid.NewGuid(), "0912345678", null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Doctor.SpecialtyNotFound", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_ShouldSucceed_AndGenerateCode_AndJoinSpecialtyName()
    {
        var (doctors, specialties) = CreateServices(out _);
        var specialtyId = await SeedSpecialtyAsync(specialties, "Tim mạch");

        var result = await doctors.CreateAsync(
            new CreateDoctorRequest("  Trần B  ", specialtyId, null, "  b@clinic.vn  "));

        Assert.True(result.IsSuccess);
        Assert.Equal("BS-000001", result.Value.Code);
        Assert.Equal("Trần B", result.Value.FullName);
        Assert.Equal("b@clinic.vn", result.Value.Email);
        Assert.Equal("Tim mạch", result.Value.SpecialtyName);
    }

    [Fact]
    public async Task GetListAsync_ShouldReturnDoctors_WithSpecialtyName()
    {
        var (doctors, specialties) = CreateServices(out _);
        var specialtyId = await SeedSpecialtyAsync(specialties, "Da liễu");
        await doctors.CreateAsync(new CreateDoctorRequest("BS An", specialtyId, null, null));

        var list = await doctors.GetListAsync(page: 1, pageSize: 10, search: null);

        Assert.Equal(1, list.Value.TotalCount);
        Assert.Equal("Da liễu", list.Value.Items.Single().SpecialtyName);
    }

    [Fact]
    public async Task UpdateAsync_ShouldFail_WhenSpecialtyNotFound()
    {
        var (doctors, specialties) = CreateServices(out _);
        var specialtyId = await SeedSpecialtyAsync(specialties);
        var created = await doctors.CreateAsync(new CreateDoctorRequest("BS C", specialtyId, null, null));

        var result = await doctors.UpdateAsync(
            created.Value.Id, new UpdateDoctorRequest("BS C", Guid.NewGuid(), null, null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task DeleteAsync_ShouldHideDoctor_FromList()
    {
        var (doctors, specialties) = CreateServices(out _);
        var specialtyId = await SeedSpecialtyAsync(specialties);
        var created = await doctors.CreateAsync(new CreateDoctorRequest("BS D", specialtyId, null, null));

        await doctors.DeleteAsync(created.Value.Id);

        var list = await doctors.GetListAsync(page: 1, pageSize: 10, search: null);
        Assert.Equal(0, list.Value.TotalCount);
    }
}
