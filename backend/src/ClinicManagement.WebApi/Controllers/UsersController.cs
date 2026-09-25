using ClinicManagement.Application.Users;
using ClinicManagement.Application.Users.Dtos;
using ClinicManagement.Domain.Users;
using ClinicManagement.WebApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.WebApi.Controllers;

/// <summary>Quản lý người dùng — chỉ dành cho Admin. Không bao giờ lộ mật khẩu băm.</summary>
[Authorize(Roles = Roles.Admin)]
[Route("api/users")]
public sealed class UsersController : ApiControllerBase
{
    private readonly IUserManagementService _users;

    public UsersController(IUserManagementService users) => _users = users;

    /// <summary>Tạo tài khoản người dùng mới.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var result = await _users.CreateAsync(request, ct);
        return ToResponse(result, StatusCodes.Status201Created);
    }

    /// <summary>Danh sách người dùng có phân trang, tìm kiếm, lọc theo vai trò/trạng thái.</summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] UserRole? role = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false,
        CancellationToken ct = default)
    {
        var result = await _users.GetListAsync(page, pageSize, search, role, isActive, sortBy, sortDesc, ct);
        return ToResponse(result);
    }

    /// <summary>Chi tiết một người dùng theo Id.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _users.GetByIdAsync(id, ct);
        return ToResponse(result);
    }

    /// <summary>Cập nhật hồ sơ tài khoản (họ tên/vai trò/email).</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var result = await _users.UpdateAsync(id, request, ct);
        return ToResponse(result);
    }

    /// <summary>Đặt lại mật khẩu cho một tài khoản.</summary>
    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(
        Guid id, [FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var result = await _users.ResetPasswordAsync(id, request, ct);
        return ToResponse(result);
    }

    /// <summary>Khoá đăng nhập một tài khoản (không thể tự khoá chính mình).</summary>
    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var result = await _users.DeactivateAsync(id, CurrentUserId, ct);
        return ToResponse(result);
    }

    /// <summary>Mở khoá đăng nhập một tài khoản.</summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        var result = await _users.ActivateAsync(id, ct);
        return ToResponse(result);
    }

    /// <summary>Xoá mềm tài khoản (không thể tự xoá chính mình).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _users.DeleteAsync(id, CurrentUserId, ct);
        return ToResponse(result);
    }
}
