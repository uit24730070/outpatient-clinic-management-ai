using ClinicManagement.Application.Auth.Dtos;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.Auth;

public sealed class AuthService : IAuthService
{
    // Thông báo chung, không tiết lộ tài khoản tồn tại hay sai mật khẩu (chống dò tài khoản).
    private static readonly Error InvalidCredentials =
        Error.Unauthorized("Auth.InvalidCredentials", "Tên đăng nhập hoặc mật khẩu không đúng.");

    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public AuthService(
        IAppDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<Result<AuthResultDto>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var username = request.Username.Trim().ToLower();

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username, ct);

        // Tài khoản không tồn tại, đã bị khoá, hoặc sai mật khẩu đều trả cùng một lỗi.
        if (user is null || !user.IsActive)
            return InvalidCredentials;

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            return InvalidCredentials;

        var token = _tokenGenerator.Generate(user);
        var doctorId = await GetLinkedDoctorIdAsync(user.Id, ct);
        return new AuthResultDto(token.AccessToken, token.ExpiresAt, UserDto.FromEntity(user, doctorId));
    }

    public async Task<Result<UserDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
            return Error.NotFound("User.NotFound", $"Không tìm thấy người dùng với Id {id}.");

        var doctorId = await GetLinkedDoctorIdAsync(user.Id, ct);
        return UserDto.FromEntity(user, doctorId);
    }

    /// <summary>
    /// Tra cứu server-side hồ sơ bác sĩ gắn với tài khoản (nếu có).
    /// Lộ doctorId qua đây thay vì nhét claim JWT để tránh vấn đề token cũ thiếu claim (ADR 0009).
    /// </summary>
    private async Task<Guid?> GetLinkedDoctorIdAsync(Guid userId, CancellationToken ct)
    {
        return await _db.Doctors
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .Select(d => (Guid?)d.Id)
            .FirstOrDefaultAsync(ct);
    }
}
