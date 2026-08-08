namespace ClinicManagement.Domain.Users;

/// <summary>Vai trò người dùng trong hệ thống (RBAC). Lưu dưới dạng chuỗi.</summary>
public enum UserRole
{
    /// <summary>Quản trị hệ thống — toàn quyền.</summary>
    Admin = 0,

    /// <summary>Lễ tân — tiếp đón, quản lý bệnh nhân/đặt lịch.</summary>
    Receptionist = 1,

    /// <summary>Bác sĩ — khám và xem hồ sơ liên quan.</summary>
    Doctor = 2
}
