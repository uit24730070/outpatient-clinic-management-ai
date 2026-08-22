namespace ClinicManagement.Domain.Billing;

/// <summary>
/// Loại dòng hoá đơn. Lưu dạng chuỗi. Phân biệt nguồn gốc dòng để tra soát/báo cáo doanh thu.
/// </summary>
public enum InvoiceItemType
{
    /// <summary>Công khám/dịch vụ (tham chiếu bảng giá dịch vụ).</summary>
    ServiceFee,

    /// <summary>Tiền thuốc đã cấp (tham chiếu danh mục thuốc).</summary>
    Medication,

    /// <summary>Khoản khác (nhập tay).</summary>
    Other
}
