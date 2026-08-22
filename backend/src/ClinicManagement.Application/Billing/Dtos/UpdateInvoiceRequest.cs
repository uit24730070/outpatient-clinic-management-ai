namespace ClinicManagement.Application.Billing.Dtos;

/// <summary>Dữ liệu cập nhật hoá đơn (chỉ khi còn <c>Draft</c>): thay cả cụm dòng dịch vụ + ghi chú. Đơn giá snapshot lại tại thời điểm sửa.</summary>
public sealed record UpdateInvoiceRequest(
    string? Note,
    IReadOnlyList<CreateInvoiceItemRequest> Items);
