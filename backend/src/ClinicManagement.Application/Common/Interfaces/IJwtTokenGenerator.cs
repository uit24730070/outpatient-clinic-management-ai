using ClinicManagement.Domain.Users;

namespace ClinicManagement.Application.Common.Interfaces;

/// <summary>Kết quả phát token: chuỗi JWT và thời điểm hết hạn (UTC).</summary>
public readonly record struct JwtToken(string AccessToken, DateTimeOffset ExpiresAt);

/// <summary>Trừu tượng phát JWT cho một người dùng (hiện thực ở Infrastructure, đọc JwtSettings).</summary>
public interface IJwtTokenGenerator
{
    JwtToken Generate(User user);
}
