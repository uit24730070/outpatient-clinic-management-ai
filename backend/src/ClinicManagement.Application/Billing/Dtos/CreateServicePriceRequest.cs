using ClinicManagement.Domain.Billing;

namespace ClinicManagement.Application.Billing.Dtos;

/// <summary>Dữ liệu đầu vào để tạo mục bảng giá dịch vụ mới.</summary>
public sealed record CreateServicePriceRequest(
    string Name,
    decimal UnitPrice,
    string? Description,
    ServiceCategory Category = ServiceCategory.Other,
    ParaclinicalGroup? Group = null);
