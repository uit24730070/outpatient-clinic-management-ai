namespace ClinicManagement.Application.StockReceipts.Dtos;

/// <summary>Dữ liệu đầu vào để tạo phiếu nhập kho (kèm các dòng nhập).</summary>
public sealed record CreateStockReceiptRequest(
    string SupplierName,
    DateTimeOffset ReceivedAt,
    string? Note,
    IReadOnlyList<StockReceiptItemRequest> Items);

/// <summary>Một dòng nhập: thuốc + lô + hạn dùng + số lượng (+ đơn giá tuỳ chọn).</summary>
public sealed record StockReceiptItemRequest(
    Guid MedicationId,
    string BatchNumber,
    DateOnly ExpiryDate,
    int Quantity,
    decimal? UnitCost);
