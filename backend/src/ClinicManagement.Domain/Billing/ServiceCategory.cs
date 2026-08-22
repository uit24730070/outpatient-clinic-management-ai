namespace ClinicManagement.Domain.Billing;

/// <summary>
/// Phân loại mục bảng giá dịch vụ. Lưu dạng chuỗi. Dùng để tách dịch vụ <b>cận lâm sàng</b>
/// (xét nghiệm/chẩn đoán hình ảnh) khỏi công khám, phục vụ lọc danh mục và quyết định loại dòng
/// hoá đơn (Paraclinical) khi lập hoá đơn (ADR 0015). Dữ liệu cũ (Sprint 14) mặc định <see cref="Other"/>.
/// </summary>
public enum ServiceCategory
{
    /// <summary>Công khám/tư vấn (khám tổng quát, tái khám, khám chuyên khoa).</summary>
    Consultation,

    /// <summary>Cận lâm sàng: xét nghiệm, chẩn đoán hình ảnh, thăm dò chức năng…</summary>
    Paraclinical,

    /// <summary>Khoản khác (mặc định cho dữ liệu chưa phân loại).</summary>
    Other
}
