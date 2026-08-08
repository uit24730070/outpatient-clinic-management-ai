namespace ClinicManagement.Application.Auth.Dtos;

/// <summary>Kết quả đăng nhập: token truy cập, thời điểm hết hạn và thông tin người dùng.</summary>
public sealed record AuthResultDto(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    UserDto User);
