using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ClinicManagement.Application.Auth;
using ClinicManagement.Application.Auth.Dtos;
using ClinicManagement.Domain.Users;
using ClinicManagement.Infrastructure.Authentication;
using ClinicManagement.Shared.Results;
using Microsoft.Extensions.Options;
using UnitTests.Common;

namespace UnitTests.Auth;

public sealed class AuthServiceTests
{
    private static readonly JwtSettings Settings = new()
    {
        Issuer = "TestIssuer",
        Audience = "TestAudience",
        Key = "unit-test-secret-key-at-least-32-bytes-long!!",
        ExpiresMinutes = 60
    };

    private static AuthService CreateService(out TestDbContext db)
    {
        db = TestDbContext.CreateInMemory();
        var hasher = new BCryptPasswordHasher();
        var tokenGen = new JwtTokenGenerator(Options.Create(Settings));
        return new AuthService(db, hasher, tokenGen);
    }

    private static async Task SeedUserAsync(
        TestDbContext db, string username = "bacsi", string password = "MatKhau@1",
        UserRole role = UserRole.Doctor, bool active = true)
    {
        var hash = new BCryptPasswordHasher().Hash(password);
        var user = new User(username, hash, "Người Dùng", role, null);
        if (!active) user.Deactivate();
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task LoginAsync_ShouldFail_WhenUserNotFound()
    {
        var service = CreateService(out _);

        var result = await service.LoginAsync(new LoginRequest("khongton", "gido"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
    }

    [Fact]
    public async Task LoginAsync_ShouldFail_WhenPasswordWrong()
    {
        var service = CreateService(out var db);
        await SeedUserAsync(db, password: "DungMatKhau@1");

        var result = await service.LoginAsync(new LoginRequest("bacsi", "SaiMatKhau"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
    }

    [Fact]
    public async Task LoginAsync_ShouldFail_WhenUserInactive()
    {
        var service = CreateService(out var db);
        await SeedUserAsync(db, password: "MatKhau@1", active: false);

        var result = await service.LoginAsync(new LoginRequest("bacsi", "MatKhau@1"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
    }

    [Fact]
    public async Task LoginAsync_ShouldSucceed_AndIssueTokenWithRoleClaim()
    {
        var service = CreateService(out var db);
        await SeedUserAsync(db, username: "letan", password: "MatKhau@1", role: UserRole.Receptionist);

        var result = await service.LoginAsync(new LoginRequest("  LETAN  ", "MatKhau@1"));

        Assert.True(result.IsSuccess);
        Assert.Equal("letan", result.Value.User.Username);
        Assert.Equal("Receptionist", result.Value.User.Role);
        Assert.True(result.Value.ExpiresAt > DateTimeOffset.UtcNow);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Value.AccessToken);
        Assert.Equal("TestIssuer", jwt.Issuer);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Receptionist");
    }
}
