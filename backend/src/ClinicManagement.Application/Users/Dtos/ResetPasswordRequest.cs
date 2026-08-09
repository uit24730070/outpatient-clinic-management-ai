namespace ClinicManagement.Application.Users.Dtos;

/// <summary>Yêu cầu đặt lại mật khẩu cho một tài khoản (Admin).</summary>
public sealed record ResetPasswordRequest(string NewPassword);
