namespace ClinicManagement.WebApi.Common;

/// <summary>
/// Tên vai trò dùng cho <c>[Authorize(Roles = ...)]</c>. Khớp <see cref="ClinicManagement.Domain.Users.UserRole"/>
/// (lưu dạng chuỗi trong claim role).
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Receptionist = "Receptionist";
    public const string Doctor = "Doctor";

    /// <summary>Nhóm được phép ghi (tạo/sửa/xoá) danh mục nghiệp vụ: Admin và Lễ tân.</summary>
    public const string ManageStaff = Admin + "," + Receptionist;

    /// <summary>Nhóm được phép ghi bệnh án (phiếu khám/đơn thuốc): Bác sĩ và Admin (xem ADR 0006).</summary>
    public const string RecordEncounter = Admin + "," + Doctor;
}
