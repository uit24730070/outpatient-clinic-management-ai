namespace ClinicManagement.Domain.Billing;

/// <summary>
/// Nhóm hiển thị của một dịch vụ <b>cận lâm sàng</b> (<see cref="ServicePrice.Category"/> =
/// <see cref="ServiceCategory.Paraclinical"/>) — dùng để gom nhóm màn chỉ định thay vì liệt kê phẳng
/// hàng chục dịch vụ (phản hồi giảng viên: chỉ định CLS dạng danh sách phẳng gây rối mắt). Lưu dạng
/// chuỗi; <c>null</c> với dịch vụ không phải Paraclinical hoặc chưa phân nhóm (rơi vào "Khác" khi hiển thị).
/// </summary>
public enum ParaclinicalGroup
{
    /// <summary>Xét nghiệm (máu, nước tiểu, sinh hoá, miễn dịch…).</summary>
    LabTest,

    /// <summary>Chẩn đoán hình ảnh (X-quang, siêu âm, CT, MRI…).</summary>
    Imaging,

    /// <summary>Thăm dò chức năng (điện tâm đồ, đo chức năng hô hấp…).</summary>
    Functional,

    /// <summary>Nội soi.</summary>
    Endoscopy,

    /// <summary>Nhóm khác/chưa phân loại.</summary>
    Other
}
