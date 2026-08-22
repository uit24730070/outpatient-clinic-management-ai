namespace ClinicManagement.Domain.Billing;

/// <summary>
/// Một dòng của hoá đơn (<see cref="Invoice"/>). Là <b>owned entity</b> — không có vòng đời độc lập,
/// luôn tạo/đọc theo cả cụm cùng hoá đơn (như <c>PrescriptionItem</c>/<c>StockReceiptItem</c>, ADR 0006/0011).
/// <para>
/// <see cref="UnitPrice"/> được <b>snapshot</b> tại thời điểm lập hoá đơn (đổi bảng giá/giá bán thuốc
/// sau không ảnh hưởng hoá đơn cũ). <see cref="LineTotal"/> = <see cref="UnitPrice"/> × <see cref="Quantity"/>.
/// </para>
/// </summary>
public class InvoiceItem
{
    // EF Core cần constructor không tham số.
    private InvoiceItem() { }

    public InvoiceItem(
        InvoiceItemType itemType,
        string description,
        decimal unitPrice,
        int quantity,
        Guid? referenceId = null)
    {
        ItemType = itemType;
        Description = description;
        UnitPrice = unitPrice;
        Quantity = quantity;
        ReferenceId = referenceId;
        LineTotal = unitPrice * quantity;
    }

    /// <summary>Loại dòng (công khám/thuốc/khác).</summary>
    public InvoiceItemType ItemType { get; private set; }

    /// <summary>Diễn giải dòng (tên dịch vụ/thuốc hoặc mô tả tự do).</summary>
    public string Description { get; private set; } = null!;

    /// <summary>Đơn giá (VND) — snapshot lúc lập hoá đơn.</summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>Số lượng (&gt; 0).</summary>
    public int Quantity { get; private set; }

    /// <summary>Thành tiền = <see cref="UnitPrice"/> × <see cref="Quantity"/>.</summary>
    public decimal LineTotal { get; private set; }

    /// <summary>
    /// Tham chiếu nguồn gốc (tuỳ chọn): Id dịch vụ (ServiceFee) hoặc Id thuốc (Medication).
    /// Chỉ để tra soát, KHÔNG dùng để join giá "sống".
    /// </summary>
    public Guid? ReferenceId { get; private set; }
}
