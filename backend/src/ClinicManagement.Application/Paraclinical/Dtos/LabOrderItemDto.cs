using ClinicManagement.Domain.Paraclinical;

namespace ClinicManagement.Application.Paraclinical.Dtos;

/// <summary>Dữ liệu một mục chỉ định cận lâm sàng trả về cho client.</summary>
public sealed record LabOrderItemDto(
    Guid Id,
    Guid ServicePriceId,
    string ServiceName,
    decimal UnitPrice,
    string? ResultText,
    string? Conclusion,
    LabOrderItemStatus Status,
    DateTimeOffset? ResultedAt)
{
    public static LabOrderItemDto FromEntity(LabOrderItem i) => new(
        i.Id, i.ServicePriceId, i.ServiceName, i.UnitPrice,
        i.ResultText, i.Conclusion, i.Status, i.ResultedAt);
}
