using ClinicManagement.Domain.Users;

namespace ClinicManagement.Application.Auth.Dtos;

/// <summary>Thông tin người dùng hiện tại trả về cho client (không chứa mật khẩu).</summary>
public sealed record UserDto(
    Guid Id,
    string Username,
    string FullName,
    string Role,
    string? Email,
    // Hồ sơ bác sĩ gắn với tài khoản (nếu là user Bác sĩ đã liên kết); null nếu chưa gắn
    // hoặc không phải bác sĩ. Cho phép FE lọc "phiếu/lịch của tôi" (ADR 0009).
    Guid? DoctorId)
{
    public static UserDto FromEntity(User user, Guid? doctorId = null) => new(
        user.Id,
        user.Username,
        user.FullName,
        user.Role.ToString(),
        user.Email,
        doctorId);
}
