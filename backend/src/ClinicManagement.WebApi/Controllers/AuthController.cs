using System.Security.Claims;
using ClinicManagement.Application.Auth;
using ClinicManagement.Application.Auth.Dtos;
using ClinicManagement.Shared.Results;
using ClinicManagement.WebApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.WebApi.Controllers;

[Route("api/auth")]
public sealed class AuthController : ApiControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Đăng nhập bằng tên đăng nhập + mật khẩu, trả JWT.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(request, ct);
        return ToResponse(result);
    }

    /// <summary>Lấy thông tin người dùng hiện tại (từ token).</summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(idClaim, out var userId))
            return ToResponse(Result.Failure<UserDto>(
                Error.Unauthorized("Auth.InvalidToken", "Token không hợp lệ.")));

        var result = await _auth.GetByIdAsync(userId, ct);
        return ToResponse(result);
    }
}
