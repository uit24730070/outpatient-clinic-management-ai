using ClinicManagement.Domain.Users;

namespace ClinicManagement.Application.Auth.Dtos;

/// <summary>Thông tin người dùng hiện tại trả về cho client (không chứa mật khẩu).</summary>
public sealed record UserDto(
    Guid Id,
    string Username,
    string FullName,
    string Role,
    string? Email)
{
    public static UserDto FromEntity(User user) => new(
        user.Id,
        user.Username,
        user.FullName,
        user.Role.ToString(),
        user.Email);
}
