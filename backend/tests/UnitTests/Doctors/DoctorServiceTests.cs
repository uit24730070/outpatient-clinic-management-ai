using ClinicManagement.Application.Doctors;
using ClinicManagement.Application.Doctors.Dtos;
using ClinicManagement.Application.Specialties;
using ClinicManagement.Application.Specialties.Dtos;
using ClinicManagement.Domain.Users;
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

    private static async Task<Guid> SeedUserAsync(TestDbContext db, UserRole role = UserRole.Doctor)
    {
        var user = new User($"user{Guid.NewGuid():N}", "hash", "Người Dùng", role, null);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    [Fact]
    public async Task LinkUserAsync_ShouldSucceed_WhenUserIsDoctorAndUnlinked()
    {
        var (doctors, specialties) = CreateServices(out var db);
        var specialtyId = await SeedSpecialtyAsync(specialties);
        var doctor = await doctors.CreateAsync(new CreateDoctorRequest("BS Gắn", specialtyId, null, null));
        var userId = await SeedUserAsync(db);

        var result = await doctors.LinkUserAsync(doctor.Value.Id, new LinkUserRequest(userId));

        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.Value.UserId);
    }

    [Fact]
    public async Task LinkUserAsync_ShouldFail_WhenUserNotDoctorRole()
    {
        var (doctors, specialties) = CreateServices(out var db);
        var specialtyId = await SeedSpecialtyAsync(specialties);
        var doctor = await doctors.CreateAsync(new CreateDoctorRequest("BS X", specialtyId, null, null));
        var userId = await SeedUserAsync(db, UserRole.Receptionist);

        var result = await doctors.LinkUserAsync(doctor.Value.Id, new LinkUserRequest(userId));

        Assert.True(result.IsFailure);
        Assert.Equal("Doctor.UserNotDoctor", result.Error.Code);
    }

    [Fact]
    public async Task LinkUserAsync_ShouldFail_WhenUserAlreadyLinkedToAnotherDoctor()
    {
        var (doctors, specialties) = CreateServices(out var db);
        var specialtyId = await SeedSpecialtyAsync(specialties);
        var doctorA = await doctors.CreateAsync(new CreateDoctorRequest("BS A", specialtyId, null, null));
        var doctorB = await doctors.CreateAsync(new CreateDoctorRequest("BS B", specialtyId, null, null));
        var userId = await SeedUserAsync(db);
        await doctors.LinkUserAsync(doctorA.Value.Id, new LinkUserRequest(userId));

        var result = await doctors.LinkUserAsync(doctorB.Value.Id, new LinkUserRequest(userId));

        Assert.True(result.IsFailure);
        Assert.Equal("Doctor.UserAlreadyLinked", result.Error.Code);
    }

    [Fact]
    public async Task LinkUserAsync_ShouldFail_WhenDoctorAlreadyHasUser()
    {
        var (doctors, specialties) = CreateServices(out var db);
        var specialtyId = await SeedSpecialtyAsync(specialties);
        var doctor = await doctors.CreateAsync(new CreateDoctorRequest("BS Y", specialtyId, null, null));
        var userId1 = await SeedUserAsync(db);
        var userId2 = await SeedUserAsync(db);
        await doctors.LinkUserAsync(doctor.Value.Id, new LinkUserRequest(userId1));

        var result = await doctors.LinkUserAsync(doctor.Value.Id, new LinkUserRequest(userId2));

        Assert.True(result.IsFailure);
        Assert.Equal("Doctor.AlreadyLinked", result.Error.Code);
    }

    [Fact]
    public async Task UnlinkUserAsync_ShouldClearUserId()
    {
        var (doctors, specialties) = CreateServices(out var db);
        var specialtyId = await SeedSpecialtyAsync(specialties);
        var doctor = await doctors.CreateAsync(new CreateDoctorRequest("BS Z", specialtyId, null, null));
        var userId = await SeedUserAsync(db);
        await doctors.LinkUserAsync(doctor.Value.Id, new LinkUserRequest(userId));

        var result = await doctors.UnlinkUserAsync(doctor.Value.Id);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.UserId);
    }
}
