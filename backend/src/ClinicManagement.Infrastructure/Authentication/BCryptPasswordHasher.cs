using ClinicManagement.Application.Common.Interfaces;

namespace ClinicManagement.Infrastructure.Authentication;

/// <summary>Băm & xác minh mật khẩu bằng BCrypt (salt tự sinh, work factor mặc định).</summary>
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string passwordHash) =>
        BCrypt.Net.BCrypt.Verify(password, passwordHash);
}
