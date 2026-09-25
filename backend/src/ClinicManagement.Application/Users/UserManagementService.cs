using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Application.Users.Dtos;
using ClinicManagement.Domain.Users;
using ClinicManagement.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.Users;

public sealed class UserManagementService : IUserManagementService
{
    private const int MaxPageSize = 100;
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public UserManagementService(IAppDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<UserListItemDto>> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var username = request.Username.Trim();

        // Unique index tính cả bản ghi đã xoá mềm (ADR 0003) → kiểm tra IgnoreQueryFilters cho thông báo thân thiện.
        var exists = await _db.Users.IgnoreQueryFilters()
            .AnyAsync(u => u.Username.ToLower() == username.ToLower(), ct);
        if (exists)
            return Error.Conflict("User.UsernameTaken", $"Tên đăng nhập '{username}' đã tồn tại.");

        var user = new User(
            username,
            _passwordHasher.Hash(request.Password),
            request.FullName.Trim(),
            request.Role,
            NormalizeOptional(request.Email));

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return (await ProjectByIdAsync(user.Id, ct))!;
    }

    public async Task<Result<PagedResult<UserListItemDto>>> GetListAsync(
        int page, int pageSize, string? search, UserRole? role, bool? isActive,
        string? sortBy = null, bool sortDesc = false, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > MaxPageSize ? 20 : pageSize;

        var query = _db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                u.Username.ToLower().Contains(term) ||
                u.FullName.ToLower().Contains(term) ||
                (u.Email != null && u.Email.ToLower().Contains(term)));
        }

        if (role is not null)
            query = query.Where(u => u.Role == role);

        if (isActive is not null)
            query = query.Where(u => u.IsActive == isActive);

        var total = await query.CountAsync(ct);
        var items = await Project(ApplySort(query, sortBy, sortDesc))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<UserListItemDto>(items, page, pageSize, total);
    }

    /// <summary>Sắp xếp theo cột do FE chọn (danh sách trắng); mặc định CreatedAt desc khi không chỉ định.</summary>
    private static IOrderedQueryable<User> ApplySort(IQueryable<User> query, string? sortBy, bool desc) =>
        sortBy switch
        {
            "username" => desc ? query.OrderByDescending(u => u.Username) : query.OrderBy(u => u.Username),
            "fullName" => desc ? query.OrderByDescending(u => u.FullName) : query.OrderBy(u => u.FullName),
            "role" => desc ? query.OrderByDescending(u => u.Role) : query.OrderBy(u => u.Role),
            _ => query.OrderByDescending(u => u.CreatedAt),
        };

    public async Task<Result<UserListItemDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await ProjectByIdAsync(id, ct);
        return dto is null
            ? Error.NotFound("User.NotFound", $"Không tìm thấy người dùng với Id {id}.")
            : dto;
    }

    public async Task<Result<UserListItemDto>> UpdateAsync(
        Guid id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
            return Error.NotFound("User.NotFound", $"Không tìm thấy người dùng với Id {id}.");

        user.UpdateDetails(request.FullName.Trim(), request.Role, NormalizeOptional(request.Email));
        await _db.SaveChangesAsync(ct);

        return (await ProjectByIdAsync(user.Id, ct))!;
    }

    public async Task<Result> ResetPasswordAsync(Guid id, ResetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
            return Result.Failure(Error.NotFound("User.NotFound", $"Không tìm thấy người dùng với Id {id}."));

        user.ChangePassword(_passwordHasher.Hash(request.NewPassword));
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeactivateAsync(Guid id, Guid currentUserId, CancellationToken ct = default)
    {
        if (id == currentUserId)
            return Result.Failure(Error.Validation("User.CannotDeactivateSelf",
                "Không thể tự khoá tài khoản đang đăng nhập."));

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
            return Result.Failure(Error.NotFound("User.NotFound", $"Không tìm thấy người dùng với Id {id}."));

        user.Deactivate();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ActivateAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
            return Result.Failure(Error.NotFound("User.NotFound", $"Không tìm thấy người dùng với Id {id}."));

        user.Activate();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, Guid currentUserId, CancellationToken ct = default)
    {
        if (id == currentUserId)
            return Result.Failure(Error.Validation("User.CannotDeleteSelf",
                "Không thể tự xoá tài khoản đang đăng nhập."));

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
            return Result.Failure(Error.NotFound("User.NotFound", $"Không tìm thấy người dùng với Id {id}."));

        // Gỡ liên kết hồ sơ bác sĩ (nếu có) để không còn tham chiếu tới tài khoản đã xoá.
        var linkedDoctor = await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == id, ct);
        linkedDoctor?.UnassignUser();

        user.MarkAsDeleted();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <summary>Ánh xạ truy vấn User sang DTO kèm doctorId liên kết (subquery).</summary>
    private IQueryable<UserListItemDto> Project(IQueryable<User> query) =>
        query.Select(u => new UserListItemDto(
            u.Id,
            u.Username,
            u.FullName,
            u.Role.ToString(),
            u.Email,
            u.IsActive,
            _db.Doctors.Where(d => d.UserId == u.Id).Select(d => (Guid?)d.Id).FirstOrDefault(),
            u.CreatedAt,
            u.UpdatedAt));

    private async Task<UserListItemDto?> ProjectByIdAsync(Guid id, CancellationToken ct) =>
        await Project(_db.Users.AsNoTracking().Where(u => u.Id == id)).FirstOrDefaultAsync(ct);

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
