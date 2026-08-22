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

    /// <summary>Nhóm được phép ghi (tạo/sửa/xoá) nghiệp vụ vận hành (Bệnh nhân, Lịch khám): Admin và Lễ tân.</summary>
    public const string ManageStaff = Admin + "," + Receptionist;

    /// <summary>Nhóm được phép ghi danh mục master Bác sĩ/Chuyên khoa: <b>chỉ Admin</b> (đọc vẫn mở cho mọi vai trò để đặt lịch).</summary>
    public const string ManageCatalog = Admin;

    /// <summary>Nhóm được phép ghi bệnh án (phiếu khám/đơn thuốc): Bác sĩ và Admin (xem ADR 0006).</summary>
    public const string RecordEncounter = Admin + "," + Doctor;
}
