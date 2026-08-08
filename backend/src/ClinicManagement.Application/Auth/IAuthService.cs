using ClinicManagement.Application.Auth.Dtos;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Application.Auth;

public interface IAuthService
{
    /// <summary>Đăng nhập: xác minh tài khoản/mật khẩu và phát JWT.</summary>
    Task<Result<AuthResultDto>> LoginAsync(LoginRequest request, CancellationToken ct = default);

    /// <summary>Lấy thông tin người dùng theo Id (cho endpoint /me).</summary>
    Task<Result<UserDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
}
