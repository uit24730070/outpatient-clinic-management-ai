namespace ClinicManagement.Application.Billing.Dtos;

/// <summary>
/// Dữ liệu đầu vào để tạo hoá đơn dịch vụ (không gắn phiếu khám). Mỗi dòng tham chiếu một dịch vụ trong bảng giá.
/// Dùng cho lập hoá đơn <b>lúc tiếp đón</b> (khám/tái khám/chỉ-CLS): gắn <see cref="AppointmentId"/> khi có lượt,
/// để null với bệnh nhân vãng lai/chỉ-CLS chưa gắn lượt.
/// </summary>
public sealed record CreateInvoiceRequest(
    Guid PatientId,
    string? Note,
    IReadOnlyList<CreateInvoiceItemRequest> Items,
    Guid? AppointmentId = null);

/// <summary>Một dòng dịch vụ khi tạo hoá đơn lẻ. Đơn giá được snapshot từ bảng giá tại thời điểm lập.</summary>
public sealed record CreateInvoiceItemRequest(
    Guid ServicePriceId,
    int Quantity);
