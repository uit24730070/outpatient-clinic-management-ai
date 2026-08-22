namespace ClinicManagement.Domain.Billing;

/// <summary>
/// Phương thức thu tiền hoá đơn. Lưu dạng chuỗi. Đặt khi hoá đơn chuyển sang <see cref="InvoiceStatus.Paid"/>.
/// </summary>
public enum PaymentMethod
{
    /// <summary>Tiền mặt.</summary>
    Cash,

    /// <summary>Thẻ (quẹt POS).</summary>
    Card,

    /// <summary>Chuyển khoản.</summary>
    Transfer
}
