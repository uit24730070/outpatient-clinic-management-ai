using ClinicManagement.Domain.Common;

namespace ClinicManagement.Domain.Users;

/// <summary>
/// Tài khoản người dùng đăng nhập hệ thống. <see cref="Username"/> là định danh đăng nhập duy nhất.
/// Phân biệt <see cref="IsActive"/> (khoá đăng nhập, tài khoản còn tồn tại) với xoá mềm
/// (<see cref="Entity.IsDeleted"/>) — đăng nhập bị chặn ở cả hai trạng thái.
/// </summary>
public class User : Entity
{
    // EF Core cần constructor không tham số.
    private User() { }

    public User(
        string username,
        string passwordHash,
        string fullName,
        UserRole role,
        string? email)
    {
        Username = username;
        PasswordHash = passwordHash;
        FullName = fullName;
        Role = role;
        Email = email;
        IsActive = true;
    }

    /// <summary>Tên đăng nhập duy nhất (không phân biệt hoa/thường khi kiểm tra).</summary>
    public string Username { get; private set; } = null!;

    /// <summary>Mật khẩu đã băm (BCrypt). Không bao giờ lộ ra ngoài.</summary>
    public string PasswordHash { get; private set; } = null!;

    public string FullName { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public string? Email { get; private set; }

    /// <summary>Tài khoản có được phép đăng nhập hay không (khoá mềm, khác xoá mềm).</summary>
    public bool IsActive { get; private set; }

    /// <summary>Đổi mật khẩu (đã băm sẵn ở lớp trên).</summary>
    public void ChangePassword(string newPasswordHash) => PasswordHash = newPasswordHash;

    /// <summary>Khoá đăng nhập.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Mở khoá đăng nhập.</summary>
    public void Activate() => IsActive = true;
}
