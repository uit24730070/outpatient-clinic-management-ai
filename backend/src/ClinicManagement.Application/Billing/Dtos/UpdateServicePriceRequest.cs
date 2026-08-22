namespace ClinicManagement.Application.Billing.Dtos;

/// <summary>Dữ liệu đầu vào để cập nhật mục bảng giá dịch vụ.</summary>
public sealed record UpdateServicePriceRequest(
    string Name,
    decimal UnitPrice,
    string? Description);
