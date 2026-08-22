namespace ClinicManagement.Application.Billing;

/// <summary>
/// Cấu hình viện phí (section "Billing"). Hiện chỉ chốt mã dịch vụ công khám mặc định dùng khi
/// lập hoá đơn từ phiếu khám (ADR 0014). Đăng ký như một singleton POCO ở Infrastructure —
/// KHÔNG dùng <c>IOptions</c> để tránh thêm phụ thuộc package cho lớp Application.
/// </summary>
public sealed class BillingOptions
{
    public const string SectionName = "Billing";

    /// <summary>Mã dịch vụ (bảng giá) dùng cho dòng "công khám" khi lập hoá đơn từ phiếu khám. Mặc định DV-000001.</summary>
    public string DefaultConsultationServiceCode { get; set; } = "DV-000001";
}
