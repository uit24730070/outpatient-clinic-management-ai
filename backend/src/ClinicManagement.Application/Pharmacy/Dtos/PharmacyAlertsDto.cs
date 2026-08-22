namespace ClinicManagement.Application.Pharmacy.Dtos;

/// <summary>Cảnh báo tồn thấp: một thuốc có tồn tổng ≤ ngưỡng đặt lại (<c>ReorderLevel</c>).</summary>
public sealed record LowStockAlertDto(
    Guid MedicationId,
    string Code,
    string Name,
    string Unit,
    int StockOnHand,
    int ReorderLevel);

/// <summary>Cảnh báo lô sắp/đã hết hạn: một lô còn tồn có hạn dùng trong ngưỡng ngày tới (hoặc đã qua).</summary>
public sealed record ExpiringBatchAlertDto(
    Guid BatchId,
    Guid MedicationId,
    string MedicationCode,
    string MedicationName,
    string BatchNumber,
    DateOnly ExpiryDate,
    int QuantityOnHand,
    bool IsExpired);

/// <summary>Tổng hợp cảnh báo kho: danh sách tồn thấp + danh sách lô sắp/đã hết hạn.</summary>
public sealed record PharmacyAlertsDto(
    IReadOnlyList<LowStockAlertDto> LowStock,
    IReadOnlyList<ExpiringBatchAlertDto> ExpiringBatches,
    int ExpiringInDays);
