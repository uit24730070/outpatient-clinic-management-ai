namespace ClinicManagement.Application.Billing.Dtos;

/// <summary>
/// Dữ liệu đầu vào để tạo hoá đơn dịch vụ (không gắn phiếu khám). Mỗi dòng tham chiếu một dịch vụ trong bảng giá.
/// Dùng cho lập hoá đơn <b>lúc tiếp đón</b> (khám/tái khám/chỉ-CLS): gắn <see cref="AppointmentIds"/> khi có lượt,
/// để trống với bệnh nhân vãng lai/chỉ-CLS chưa gắn lượt.
/// </summary>
/// <param name="AppointmentIds">
/// Các dịch vụ khám (Lượt tiếp đón) được thu trong hoá đơn này — có thể nhiều hơn 1 khi lập hoá đơn ở
/// <b>cấp Lượt</b> gộp nhiều dịch vụ khám cùng lúc (ADR 0021 mở rộng). Mỗi dịch vụ khám chỉ lập được một lần
/// (<c>Appointment.InvoicedAt</c>, đã lập rồi → 409); phần tử đầu tiên được gắn làm <c>Invoice.AppointmentId</c>
/// (khoá gom cũ, giữ tương thích lọc/hiển thị theo lịch). Để trống với bệnh nhân vãng lai/chỉ-CLS.
/// </param>
/// <param name="LabOrderId">
/// Gộp thêm dòng phí cận lâm sàng từ một phiếu chỉ định chưa lập hoá đơn (tuỳ chọn) — dòng được nạp tự động
/// từ <c>LabOrder.Items</c> (không cần truyền lại qua <see cref="Items"/>). Gắn <c>Invoice.LabOrderId</c> để
/// móc thu tiền → mở cổng nhập kết quả CLS hoạt động đúng như lập riêng (ADR 0021, PAY-01).
/// </param>
public sealed record CreateInvoiceRequest(
    Guid PatientId,
    string? Note,
    IReadOnlyList<CreateInvoiceItemRequest> Items,
    IReadOnlyList<Guid>? AppointmentIds = null,
    Guid? LabOrderId = null);

/// <summary>Một dòng dịch vụ khi tạo hoá đơn lẻ. Đơn giá được snapshot từ bảng giá tại thời điểm lập.</summary>
public sealed record CreateInvoiceItemRequest(
    Guid ServicePriceId,
    int Quantity);
