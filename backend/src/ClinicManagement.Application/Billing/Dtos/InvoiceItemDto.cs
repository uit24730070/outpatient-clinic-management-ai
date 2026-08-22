using ClinicManagement.Domain.Billing;

namespace ClinicManagement.Application.Billing.Dtos;

/// <summary>Dữ liệu một dòng hoá đơn trả về cho client.</summary>
public sealed record InvoiceItemDto(
    InvoiceItemType ItemType,
    string Description,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    Guid? ReferenceId)
{
    public static InvoiceItemDto FromEntity(InvoiceItem i) => new(
        i.ItemType, i.Description, i.UnitPrice, i.Quantity, i.LineTotal, i.ReferenceId);
}
