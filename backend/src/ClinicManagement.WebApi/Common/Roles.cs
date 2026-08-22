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
    public const string Pharmacist = "Pharmacist";

    /// <summary>Nhóm được phép ghi (tạo/sửa/xoá) nghiệp vụ vận hành (Bệnh nhân, Lịch khám): Admin và Lễ tân.</summary>
    public const string ManageStaff = Admin + "," + Receptionist;

    /// <summary>Nhóm được phép ghi danh mục master Bác sĩ/Chuyên khoa: <b>chỉ Admin</b> (đọc vẫn mở cho mọi vai trò để đặt lịch).</summary>
    public const string ManageCatalog = Admin;

    /// <summary>Nhóm được phép ghi bệnh án (phiếu khám/đơn thuốc): Bác sĩ và Admin (xem ADR 0006).</summary>
    public const string RecordEncounter = Admin + "," + Doctor;

    /// <summary>
    /// Nhóm được phép ghi nghiệp vụ kho thuốc (danh mục, nhập kho, sổ cái, cảnh báo): Admin và Dược sĩ.
    /// Kho thuốc do Dược sĩ quản lý trực tiếp — Lễ tân/Bác sĩ không còn quản lý (ADR 0013).
    /// Đọc danh mục vẫn mở cho mọi vai trò (bác sĩ tra khi kê đơn).
    /// </summary>
    public const string ManagePharmacy = Admin + "," + Pharmacist;

    /// <summary>
    /// Nhóm được phép chốt phiếu khám (kèm cấp phát thuốc FEFO): Admin, Bác sĩ và Dược sĩ.
    /// Vì cấp phát gộp vào bước chốt phiếu (ADR 0011), bác sĩ (người khám) và dược sĩ (quầy phát thuốc)
    /// đều cần thực hiện được; Lễ tân không đụng tồn kho nữa (ADR 0013).
    /// </summary>
    public const string DispenseEncounter = Admin + "," + Doctor + "," + Pharmacist;

    /// <summary>
    /// Nhóm được phép ghi/đọc nghiệp vụ viện phí (bảng giá dịch vụ, hoá đơn, thu tiền): Admin và Lễ tân (ADR 0014).
    /// Bác sĩ/Dược sĩ không thấy dữ liệu tài chính.
    /// </summary>
    public const string ManageBilling = Admin + "," + Receptionist;
}
