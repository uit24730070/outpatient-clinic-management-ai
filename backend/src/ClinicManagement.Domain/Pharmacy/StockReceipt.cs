using ClinicManagement.Domain.Common;

namespace ClinicManagement.Domain.Pharmacy;

/// <summary>
/// Phiếu nhập kho: chứng từ làm tăng tồn theo lô. Mã phiếu (<see cref="Code"/>) là định danh nghiệp vụ.
/// Là <b>aggregate root</b> của cụm dòng nhập (<see cref="Items"/>, owned collection). Phiếu nhập
/// <b>bất biến sau khi tạo</b> ở P1 (không sửa/xoá — tránh phải hoàn tác tồn); chỉ đọc. Việc cộng/tạo lô
/// và ghi sổ cái (<see cref="StockTransaction"/>) do tầng service thực hiện trong cùng một SaveChanges.
/// </summary>
public class StockReceipt : Entity
{
    private readonly List<StockReceiptItem> _items = new();

    // EF Core cần constructor không tham số.
    private StockReceipt() { }

    public StockReceipt(
        string code,
        string supplierName,
        DateTimeOffset receivedAt,
        string? note,
        IEnumerable<StockReceiptItem> items)
    {
        Code = code;
        SupplierName = supplierName;
        ReceivedAt = receivedAt;
        Note = note;
        _items.AddRange(items);
    }

    /// <summary>Mã phiếu nhập duy nhất, ví dụ PN-000001.</summary>
    public string Code { get; private set; } = null!;

    /// <summary>Nhà cung cấp (văn bản tự do ở P1).</summary>
    public string SupplierName { get; private set; } = null!;

    /// <summary>Thời điểm nhận hàng.</summary>
    public DateTimeOffset ReceivedAt { get; private set; }

    /// <summary>Ghi chú (tuỳ chọn).</summary>
    public string? Note { get; private set; }

    /// <summary>Cụm dòng nhập (chỉ đọc từ ngoài).</summary>
    public IReadOnlyCollection<StockReceiptItem> Items => _items.AsReadOnly();
}
