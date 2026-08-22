namespace ClinicManagement.Application.StockReceipts.Dtos;

/// <summary>Dữ liệu phiếu nhập kho trả về cho client, kèm cụm dòng nhập.</summary>
public sealed record StockReceiptDto(
    Guid Id,
    string Code,
    string SupplierName,
    DateTimeOffset ReceivedAt,
    string? Note,
    IReadOnlyList<StockReceiptItemDto> Items,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
