namespace ClinicManagement.Domain.Billing;

/// <summary>
/// Trạng thái hoá đơn. Lưu dạng chuỗi (đồng nhất với các enum khác trong hệ thống).
/// Vòng đời: <see cref="Draft"/> → <see cref="Paid"/> hoặc <see cref="Cancelled"/> (xem ADR 0014).
/// </summary>
public enum InvoiceStatus
{
    /// <summary>Nháp — được sửa dòng, thu tiền hoặc huỷ.</summary>
    Draft,

    /// <summary>Đã thu tiền — bất biến (không sửa/huỷ/xoá).</summary>
    Paid,

    /// <summary>Đã huỷ — bất biến.</summary>
    Cancelled
}
