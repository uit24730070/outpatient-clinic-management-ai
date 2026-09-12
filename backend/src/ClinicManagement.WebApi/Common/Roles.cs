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
    public const string Technician = "Technician";
    public const string Nurse = "Nurse";

    /// <summary>Nhóm được phép ghi (tạo/sửa/xoá) nghiệp vụ vận hành (Bệnh nhân, Lịch khám): Admin và Lễ tân.</summary>
    public const string ManageStaff = Admin + "," + Receptionist;

    /// <summary>Nhóm được phép ghi danh mục master Bác sĩ/Chuyên khoa: <b>chỉ Admin</b> (đọc vẫn mở cho mọi vai trò để đặt lịch).</summary>
    public const string ManageCatalog = Admin;

    /// <summary>Nhóm được phép ghi bệnh án (phiếu khám/đơn thuốc): Bác sĩ và Admin (xem ADR 0006).</summary>
    public const string RecordEncounter = Admin + "," + Doctor;

    /// <summary>
    /// Nhóm được phép tự bắt đầu khám (CheckedIn → InProgress): Admin, Lễ tân và <b>Bác sĩ</b> (Epic 17,
    /// UX-05) — Bác sĩ tự chuyển trạng thái ngay tại workspace của mình thay vì phải chờ Lễ tân/Điều
    /// dưỡng thao tác ở màn khác. Check-in/hoàn tất/huỷ vẫn thuộc <see cref="ManageStaff"/>.
    /// </summary>
    public const string StartExam = ManageStaff + "," + Doctor;

    /// <summary>
    /// Nhóm được phép ghi nghiệp vụ kho thuốc (danh mục, nhập kho, sổ cái, cảnh báo): Admin và Dược sĩ.
    /// Kho thuốc do Dược sĩ quản lý trực tiếp — Lễ tân/Bác sĩ không còn quản lý (ADR 0013).
    /// Đọc danh mục vẫn mở cho mọi vai trò (bác sĩ tra khi kê đơn).
    /// </summary>
    public const string ManagePharmacy = Admin + "," + Pharmacist;

    /// <summary>
    /// Nhóm được phép ghi nghiệp vụ viện phí (hoá đơn, thu tiền): Admin và Lễ tân (ADR 0014).
    /// Riêng đọc bảng giá dịch vụ (<c>GET /api/service-prices</c>) mở cho mọi vai trò đã đăng nhập —
    /// Bác sĩ cần tra giá khi chỉ định cận lâm sàng (xem <c>LabOrderPanel</c>).
    /// </summary>
    public const string ManageBilling = Admin + "," + Receptionist;

    /// <summary>
    /// Nhóm được phép nhập kết quả cận lâm sàng: Admin, Bác sĩ và Kỹ thuật viên (ADR 0016).
    /// Tách khỏi <see cref="RecordEncounter"/> để Kỹ thuật viên thực hiện & nhập kết quả CLS mà không
    /// đụng bệnh án/đơn thuốc. Chỉ định (encounter) vẫn là Bác sĩ; chỉ định walk-in là Lễ tân.
    /// </summary>
    public const string RecordLabResult = Admin + "," + Doctor + "," + Technician;

    /// <summary>
    /// Nhóm được phép nhập/cập nhật sinh hiệu (vitals) sau tiếp đón: Admin và Điều dưỡng (ADR 0019).
    /// Bác sĩ chỉ <b>đọc</b> sinh hiệu trong bệnh án (không ghi qua vai trò này).
    /// </summary>
    public const string RecordVitals = Admin + "," + Nurse;

    /// <summary>
    /// Nhóm được phép điều phối hàng đợi khám (lấy số, gọi số, chuyển trạng thái vé): Admin, Lễ tân và
    /// Điều dưỡng (ADR 0019). Lễ tân lấy số khi tiếp đón; Điều dưỡng gọi/điều phối phòng.
    /// </summary>
    public const string ManageQueue = Admin + "," + Receptionist + "," + Nurse;
}
