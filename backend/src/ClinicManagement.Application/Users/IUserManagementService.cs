using ClinicManagement.Application.Users.Dtos;
using ClinicManagement.Domain.Users;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Application.Users;

/// <summary>Quản lý vòng đời tài khoản người dùng (chỉ Admin). Bao gồm gắn/gỡ liên kết User↔Doctor.</summary>
public interface IUserManagementService
{
    Task<Result<UserListItemDto>> CreateAsync(CreateUserRequest request, CancellationToken ct = default);

    Task<Result<PagedResult<UserListItemDto>>> GetListAsync(
        int page, int pageSize, string? search, UserRole? role, bool? isActive, CancellationToken ct = default);

    Task<Result<UserListItemDto>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Result<UserListItemDto>> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default);

    Task<Result> ResetPasswordAsync(Guid id, ResetPasswordRequest request, CancellationToken ct = default);

    /// <summary>Khoá đăng nhập. <paramref name="currentUserId"/> để chặn tự khoá chính mình.</summary>
    Task<Result> DeactivateAsync(Guid id, Guid currentUserId, CancellationToken ct = default);

    Task<Result> ActivateAsync(Guid id, CancellationToken ct = default);

    /// <summary>Xoá mềm tài khoản. <paramref name="currentUserId"/> để chặn tự xoá chính mình.</summary>
    Task<Result> DeleteAsync(Guid id, Guid currentUserId, CancellationToken ct = default);
}
