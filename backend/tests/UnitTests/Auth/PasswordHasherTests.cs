using ClinicManagement.Infrastructure.Authentication;

namespace UnitTests.Auth;

public sealed class PasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_ThenVerify_ShouldSucceed_ForCorrectPassword()
    {
        var hash = _hasher.Hash("Admin@123");

        Assert.NotEqual("Admin@123", hash);          // đã băm, không lưu thô
        Assert.True(_hasher.Verify("Admin@123", hash));
    }

    [Fact]
    public void Verify_ShouldFail_ForWrongPassword()
    {
        var hash = _hasher.Hash("Admin@123");

        Assert.False(_hasher.Verify("SaiMatKhau", hash));
    }
}
