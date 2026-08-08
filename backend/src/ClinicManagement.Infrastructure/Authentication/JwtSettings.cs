namespace ClinicManagement.Infrastructure.Authentication;

/// <summary>Cấu hình phát/kiểm JWT, đọc từ section "Jwt" trong cấu hình ứng dụng.</summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    /// <summary>Khoá bí mật ký HS256 — tối thiểu 32 byte. Không commit giá trị thật.</summary>
    public string Key { get; set; } = string.Empty;

    public int ExpiresMinutes { get; set; } = 120;
}
