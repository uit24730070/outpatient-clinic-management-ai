namespace ClinicManagement.Domain.Users;

/// <summary>Vai trò người dùng trong hệ thống (RBAC). Lưu dưới dạng chuỗi.</summary>
public enum UserRole
{
    /// <summary>Quản trị hệ thống — toàn quyền.</summary>
    Admin = 0,

    /// <summary>Lễ tân — tiếp nhận, quản lý bệnh nhân/đặt lịch.</summary>
    Receptionist = 1,

    /// <summary>Bác sĩ — khám và xem hồ sơ liên quan.</summary>
    Doctor = 2,

    /// <summary>Dược sĩ — quản lý trực tiếp kho thuốc (danh mục, nhập kho, sổ cái, cảnh báo, cấp phát).</summary>
    Pharmacist = 3,

    /// <summary>Kỹ thuật viên — thực hiện dịch vụ cận lâm sàng và nhập kết quả (ADR 0016).</summary>
    Technician = 4,

    /// <summary>Điều dưỡng — nhập sinh hiệu sau tiếp nhận và điều phối hàng đợi khám (ADR 0019).</summary>
    Nurse = 5
}
