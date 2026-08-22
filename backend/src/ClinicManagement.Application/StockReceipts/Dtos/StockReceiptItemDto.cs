namespace ClinicManagement.Application.StockReceipts.Dtos;

/// <summary>Một dòng của phiếu nhập, kèm tên thuốc để hiển thị.</summary>
public sealed record StockReceiptItemDto(
    Guid MedicationId,
    string? MedicationName,
    string BatchNumber,
    DateOnly ExpiryDate,
    int Quantity,
    decimal? UnitCost);
