namespace ClinicManagement.Application.Billing.Dtos;

/// <summary>Dữ liệu đầu vào để tạo hoá đơn dịch vụ lẻ (không gắn phiếu khám). Mỗi dòng tham chiếu một dịch vụ trong bảng giá.</summary>
public sealed record CreateInvoiceRequest(
    Guid PatientId,
    string? Note,
    IReadOnlyList<CreateInvoiceItemRequest> Items);

/// <summary>Một dòng dịch vụ khi tạo hoá đơn lẻ. Đơn giá được snapshot từ bảng giá tại thời điểm lập.</summary>
public sealed record CreateInvoiceItemRequest(
    Guid ServicePriceId,
    int Quantity);
