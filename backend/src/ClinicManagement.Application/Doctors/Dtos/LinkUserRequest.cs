namespace ClinicManagement.Application.Doctors.Dtos;

/// <summary>Yêu cầu gắn một tài khoản đăng nhập vào hồ sơ bác sĩ.</summary>
public sealed record LinkUserRequest(Guid UserId);
