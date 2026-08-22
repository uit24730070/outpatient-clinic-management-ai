using ClinicManagement.Domain.Billing;

namespace ClinicManagement.Application.Billing.Dtos;

/// <summary>Dữ liệu mục bảng giá dịch vụ trả về cho client.</summary>
public sealed record ServicePriceDto(
    Guid Id,
    string Code,
    string Name,
    decimal UnitPrice,
    string? Description,
    ServiceCategory Category,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public static ServicePriceDto FromEntity(ServicePrice s) => new(
        s.Id, s.Code, s.Name, s.UnitPrice, s.Description, s.Category, s.CreatedAt, s.UpdatedAt);
}
