namespace ClinicManagement.Domain.Billing;

/// <summary>
/// Trạng thái hoá đơn. Lưu dạng chuỗi (đồng nhất với các enum khác trong hệ thống).
/// Vòng đời: <see cref="Draft"/> → <see cref="Paid"/> / <see cref="Cancelled"/>;
/// <see cref="Paid"/> → <see cref="Refunded"/> (ADR 0022, REF-01).
/// </summary>
public enum InvoiceStatus
{
    /// <summary>Nháp — được sửa dòng, thu tiền hoặc huỷ.</summary>
    Draft,

    /// <summary>Đã thu tiền — chỉ chuyển sang Refunded (không sửa/huỷ/xoá).</summary>
    Paid,

    /// <summary>Đã huỷ — bất biến.</summary>
    Cancelled,

    /// <summary>Đã hoàn tiền — bất biến; không xoá để giữ vết kiểm toán (ADR 0022).</summary>
    Refunded
}
