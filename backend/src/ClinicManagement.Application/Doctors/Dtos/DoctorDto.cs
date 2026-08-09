namespace ClinicManagement.Application.Doctors.Dtos;

/// <summary>Dữ liệu bác sĩ trả về cho client, kèm tên chuyên khoa (join).</summary>
public sealed record DoctorDto(
    Guid Id,
    string Code,
    string FullName,
    Guid SpecialtyId,
    string? SpecialtyName,
    string? PhoneNumber,
    string? Email,
    // Tài khoản đăng nhập gắn với hồ sơ (null nếu chưa gắn) — cho màn quản lý người dùng/liên kết.
    Guid? UserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
