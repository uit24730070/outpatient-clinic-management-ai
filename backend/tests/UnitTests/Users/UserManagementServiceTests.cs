using ClinicManagement.Application.Users;
using ClinicManagement.Application.Users.Dtos;
using ClinicManagement.Domain.Users;
using ClinicManagement.Infrastructure.Authentication;
using ClinicManagement.Shared.Results;
using UnitTests.Common;

namespace UnitTests.Users;

public sealed class UserManagementServiceTests
{
    private static UserManagementService CreateService(out TestDbContext db)
    {
        db = TestDbContext.CreateInMemory();
        return new UserManagementService(db, new BCryptPasswordHasher());
    }

    private static CreateUserRequest ValidCreate(
        string username = "letan01", UserRole role = UserRole.Receptionist) =>
        new(username, "MatKhau@123", "Người Dùng", role, "u@clinic.vn");

    [Fact]
    public async Task CreateAsync_ShouldSucceed_AndTrimUsername_AndHashPassword()
    {
        var service = CreateService(out var db);

        var result = await service.CreateAsync(ValidCreate("  letan01  "));

        Assert.True(result.IsSuccess);
        Assert.Equal("letan01", result.Value.Username);
        Assert.True(result.Value.IsActive);

        var stored = db.Users.Single();
        Assert.NotEqual("MatKhau@123", stored.PasswordHash);
        Assert.True(new BCryptPasswordHasher().Verify("MatKhau@123", stored.PasswordHash));
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenUsernameTaken_CaseInsensitive()
    {
        var service = CreateService(out _);
        await service.CreateAsync(ValidCreate("letan01"));

        var result = await service.CreateAsync(ValidCreate("LETAN01"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("User.UsernameTaken", result.Error.Code);
    }

    [Fact]
    public async Task UpdateAsync_ShouldChangeDetails_ButNotPassword()
    {
        var service = CreateService(out var db);
        var created = await service.CreateAsync(ValidCreate("bs01", UserRole.Doctor));
        var hashBefore = db.Users.Single().PasswordHash;

        var result = await service.UpdateAsync(created.Value.Id,
            new UpdateUserRequest("Bác Sĩ Mới", UserRole.Admin, "new@clinic.vn"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Bác Sĩ Mới", result.Value.FullName);
        Assert.Equal("Admin", result.Value.Role);
        Assert.Equal(hashBefore, db.Users.Single().PasswordHash);
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldSetNewHash()
    {
        var service = CreateService(out var db);
        var created = await service.CreateAsync(ValidCreate("u1"));

        var result = await service.ResetPasswordAsync(created.Value.Id, new ResetPasswordRequest("MoiToanh@9"));

        Assert.True(result.IsSuccess);
        Assert.True(new BCryptPasswordHasher().Verify("MoiToanh@9", db.Users.Single().PasswordHash));
    }

    [Fact]
    public async Task DeactivateAsync_ShouldFail_WhenTargetIsSelf()
    {
        var service = CreateService(out _);
        var created = await service.CreateAsync(ValidCreate("admin2", UserRole.Admin));

        var result = await service.DeactivateAsync(created.Value.Id, currentUserId: created.Value.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("User.CannotDeactivateSelf", result.Error.Code);
    }

    [Fact]
    public async Task DeactivateAsync_ShouldSucceed_ForOtherUser()
    {
        var service = CreateService(out var db);
        var created = await service.CreateAsync(ValidCreate("u2"));

        var result = await service.DeactivateAsync(created.Value.Id, currentUserId: Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.False(db.Users.Single().IsActive);
    }

    [Fact]
    public async Task DeleteAsync_ShouldFail_WhenTargetIsSelf()
    {
        var service = CreateService(out _);
        var created = await service.CreateAsync(ValidCreate("u3"));

        var result = await service.DeleteAsync(created.Value.Id, currentUserId: created.Value.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("User.CannotDeleteSelf", result.Error.Code);
    }

    [Fact]
    public async Task GetListAsync_ShouldFilterByRoleAndActive()
    {
        var service = CreateService(out _);
        await service.CreateAsync(ValidCreate("letan", UserRole.Receptionist));
        var doctor = await service.CreateAsync(ValidCreate("bacsi", UserRole.Doctor));
        await service.DeactivateAsync(doctor.Value.Id, currentUserId: Guid.NewGuid());

        var onlyDoctors = await service.GetListAsync(1, 20, null, UserRole.Doctor, null);
        var onlyActive = await service.GetListAsync(1, 20, null, null, isActive: true);

        Assert.Single(onlyDoctors.Value.Items);
        Assert.Equal("bacsi", onlyDoctors.Value.Items[0].Username);
        Assert.Single(onlyActive.Value.Items);
        Assert.Equal("letan", onlyActive.Value.Items[0].Username);
    }
}
