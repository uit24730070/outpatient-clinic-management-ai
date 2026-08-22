using ClinicManagement.Domain.Common;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Domain.Billing;

/// <summary>
/// Hoá đơn viện phí. Mã hoá đơn (<see cref="Code"/>) là định danh nghiệp vụ. Lưu <b>snapshot</b>
/// bệnh nhân (<see cref="PatientId"/>) và (tuỳ chọn) gắn <b>1–1</b> với một phiếu khám
/// (<see cref="EncounterId"/>, unique). Là <b>aggregate root</b> của cụm dòng hoá đơn
/// (<see cref="Items"/>, owned collection) — dòng được thay theo cả cụm qua <see cref="ReplaceItems"/>.
/// Vòng đời: <see cref="InvoiceStatus.Draft"/> → <see cref="InvoiceStatus.Paid"/> /
/// <see cref="InvoiceStatus.Cancelled"/> (xem ADR 0014).
/// </summary>
public class Invoice : Entity
{
    private readonly List<InvoiceItem> _items = new();

    // EF Core cần constructor không tham số.
    private Invoice() { }

    public Invoice(string code, Guid patientId, Guid? encounterId, string? note, IEnumerable<InvoiceItem> items)
    {
        Code = code;
        PatientId = patientId;
        EncounterId = encounterId;
        Note = note;
        Status = InvoiceStatus.Draft;
        _items.AddRange(items);
        Recalculate();
    }

    /// <summary>Mã hoá đơn duy nhất, ví dụ HD-000001.</summary>
    public string Code { get; private set; } = null!;

    /// <summary>Bệnh nhân (snapshot).</summary>
    public Guid PatientId { get; private set; }

    /// <summary>Phiếu khám nguồn (nếu lập từ phiếu khám). Null với hoá đơn dịch vụ lẻ. Unique khi có giá trị.</summary>
    public Guid? EncounterId { get; private set; }

    public InvoiceStatus Status { get; private set; }

    /// <summary>Tổng tiền = Σ <see cref="InvoiceItem.LineTotal"/>. Tính lại mỗi khi thay dòng.</summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>Thời điểm thu tiền (null nếu chưa thu).</summary>
    public DateTimeOffset? PaidAt { get; private set; }

    /// <summary>Phương thức thu tiền (null nếu chưa thu).</summary>
    public PaymentMethod? PaymentMethod { get; private set; }

    /// <summary>Ghi chú (tuỳ chọn).</summary>
    public string? Note { get; private set; }

    /// <summary>Cụm dòng hoá đơn (chỉ đọc từ ngoài; thay cả cụm qua <see cref="ReplaceItems"/>).</summary>
    public IReadOnlyCollection<InvoiceItem> Items => _items.AsReadOnly();

    /// <summary>Thay toàn bộ cụm dòng hoá đơn và tính lại tổng. Chỉ khi còn <see cref="InvoiceStatus.Draft"/>.</summary>
    public Result ReplaceItems(IEnumerable<InvoiceItem> items)
    {
        if (Status != InvoiceStatus.Draft)
            return InvalidTransition(nameof(ReplaceItems));

        _items.Clear();
        _items.AddRange(items);
        Recalculate();
        return Result.Success();
    }

    /// <summary>Cập nhật ghi chú (không ràng buộc vòng đời — đi kèm sửa dòng ở trạng thái Draft).</summary>
    public void UpdateNote(string? note) => Note = note;

    /// <summary>Thu tiền: Draft → Paid, đặt phương thức + thời điểm. Sai vòng đời → 409.</summary>
    public Result Pay(PaymentMethod method, DateTimeOffset paidAt)
    {
        if (Status != InvoiceStatus.Draft)
            return InvalidTransition(nameof(Pay));

        Status = InvoiceStatus.Paid;
        PaymentMethod = method;
        PaidAt = paidAt;
        return Result.Success();
    }

    /// <summary>Huỷ hoá đơn: Draft → Cancelled. Sai vòng đời → 409.</summary>
    public Result Cancel()
    {
        if (Status != InvoiceStatus.Draft)
            return InvalidTransition(nameof(Cancel));

        Status = InvoiceStatus.Cancelled;
        return Result.Success();
    }

    private void Recalculate() => TotalAmount = _items.Sum(i => i.LineTotal);

    private Result InvalidTransition(string action) => Result.Failure(Error.Conflict(
        "Billing.InvalidTransition",
        $"Không thể thực hiện '{action}' khi trạng thái hoá đơn là {Status}."));
}
