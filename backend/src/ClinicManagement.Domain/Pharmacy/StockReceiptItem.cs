namespace ClinicManagement.Domain.Pharmacy;

/// <summary>
/// Một dòng của phiếu nhập kho (<see cref="StockReceipt"/>). Là <b>owned entity</b> —
/// không có vòng đời độc lập, luôn tạo/đọc theo cả phiếu. Ghi lại thuốc, lô, hạn dùng,
/// số lượng nhập và (tuỳ chọn) đơn giá nhập. Việc tăng tồn theo lô + ghi sổ cái do tầng service lo.
/// </summary>
public class StockReceiptItem
{
    // EF Core cần constructor không tham số.
    private StockReceiptItem() { }

    public StockReceiptItem(
        Guid medicationId,
        string batchNumber,
        DateOnly expiryDate,
        int quantity,
        decimal? unitCost)
    {
        MedicationId = medicationId;
        BatchNumber = batchNumber;
        ExpiryDate = expiryDate;
        Quantity = quantity;
        UnitCost = unitCost;
    }

    /// <summary>Thuốc được nhập.</summary>
    public Guid MedicationId { get; private set; }

    /// <summary>Số lô của nhà sản xuất.</summary>
    public string BatchNumber { get; private set; } = null!;

    /// <summary>Hạn dùng của lô.</summary>
    public DateOnly ExpiryDate { get; private set; }

    /// <summary>Số lượng nhập (&gt; 0).</summary>
    public int Quantity { get; private set; }

    /// <summary>Đơn giá nhập (tuỳ chọn).</summary>
    public decimal? UnitCost { get; private set; }
}
