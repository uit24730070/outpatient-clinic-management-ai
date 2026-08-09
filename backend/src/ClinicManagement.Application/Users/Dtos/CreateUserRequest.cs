using System.Text.Json.Serialization;
using ClinicManagement.Domain.Users;

namespace ClinicManagement.Application.Users.Dtos;

/// <summary>Yêu cầu tạo tài khoản người dùng mới (Admin).</summary>
/// <remarks><see cref="Role"/> nhận chuỗi ("Admin"/"Receptionist"/"Doctor") để khớp FE (khác quy ước enum-số).</remarks>
public sealed record CreateUserRequest(
    string Username,
    string Password,
    string FullName,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] UserRole Role,
    string? Email);
