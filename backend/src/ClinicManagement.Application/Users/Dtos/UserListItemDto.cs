using ClinicManagement.Domain.Users;

namespace ClinicManagement.Application.Users.Dtos;

/// <summary>Dữ liệu tài khoản cho màn quản lý người dùng (Admin). Không bao giờ chứa mật khẩu.</summary>
public sealed record UserListItemDto(
    Guid Id,
    string Username,
    string FullName,
    string Role,
    string? Email,
    bool IsActive,
    // Hồ sơ bác sĩ gắn với tài khoản (nếu là user Bác sĩ đã liên kết); null nếu chưa gắn/không phải bác sĩ.
    Guid? DoctorId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
