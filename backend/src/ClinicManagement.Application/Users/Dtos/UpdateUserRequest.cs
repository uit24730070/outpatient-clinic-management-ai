using System.Text.Json.Serialization;
using ClinicManagement.Domain.Users;

namespace ClinicManagement.Application.Users.Dtos;

/// <summary>Yêu cầu cập nhật hồ sơ tài khoản (không đổi tên đăng nhập, không đổi mật khẩu).</summary>
/// <remarks><see cref="Role"/> nhận chuỗi ("Admin"/"Receptionist"/"Doctor") để khớp FE.</remarks>
public sealed record UpdateUserRequest(
    string FullName,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] UserRole Role,
    string? Email);
