namespace ClinicManagement.Application.Auth.Dtos;

/// <summary>Yêu cầu đăng nhập bằng tên đăng nhập và mật khẩu.</summary>
public sealed record LoginRequest(string Username, string Password);
