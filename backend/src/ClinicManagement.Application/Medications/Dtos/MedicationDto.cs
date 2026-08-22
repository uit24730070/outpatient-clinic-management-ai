namespace ClinicManagement.Application.Medications.Dtos;

/// <summary>Dữ liệu thuốc trả về cho client, kèm tồn tổng (tính phía server từ các lô chưa xoá).</summary>
public sealed record MedicationDto(
    Guid Id,
    string Code,
    string Name,
    string ActiveIngredient,
    string Unit,
    int ReorderLevel,
    string? Description,
    /// <summary>Giá bán một đơn vị (VND), dùng tính tiền thuốc trên hoá đơn (BILL-02).</summary>
    decimal SalePrice,
    /// <summary>Tồn tổng = tổng QuantityOnHand các lô chưa xoá của thuốc.</summary>
    int StockOnHand,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
