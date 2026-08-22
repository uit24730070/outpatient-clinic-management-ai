using ClinicManagement.Domain.Common;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Domain.Paraclinical;

/// <summary>
/// Phiếu chỉ định cận lâm sàng. Có <b>hai nguồn phát sinh</b> (ADR 0016):
/// (1) <b>bác sĩ chỉ định trong lúc khám</b> — gắn phiếu khám nguồn (<see cref="EncounterId"/>) + bác sĩ
/// (<see cref="DoctorId"/>); (2) <b>walk-in do lễ tân đăng ký</b> — không cần phiếu khám/bác sĩ
/// (<see cref="EncounterId"/>/<see cref="DoctorId"/> null), có thể gắn lượt tiếp đón (<see cref="AppointmentId"/>).
/// Mã phiếu (<see cref="Code"/>, dạng CLS-) là định danh nghiệp vụ; lưu <b>snapshot</b> bệnh nhân/bác sĩ.
/// Là <b>aggregate root</b> của cụm mục chỉ định (<see cref="Items"/>, owned collection). Vòng đời
/// <see cref="LabOrderStatus"/> chuyển tự động theo tiến độ nhập kết quả. Phí CLS được lập <b>hoá đơn riêng</b>
/// (Mô hình A) — chống lập trùng bằng cờ <see cref="InvoicedAt"/> (ADR 0015).
/// </summary>
public class LabOrder : Entity
{
    private readonly List<LabOrderItem> _items = new();

    // EF Core cần constructor không tham số.
    private LabOrder() { }

    /// <summary>Đường bác sĩ chỉ định trong lúc khám (Sprint 15): encounter/doctor bắt buộc.</summary>
    public LabOrder(
        string code, Guid encounterId, Guid patientId, Guid doctorId, string? note,
        IEnumerable<LabOrderItem> items)
    {
        Code = code;
        EncounterId = encounterId;
        PatientId = patientId;
        DoctorId = doctorId;
        Note = note;
        Status = LabOrderStatus.Ordered;
        _items.AddRange(items);
    }

    /// <summary>
    /// Đường <b>walk-in</b> do lễ tân đăng ký (ADR 0016): không có phiếu khám/bác sĩ; có thể gắn lượt tiếp đón.
    /// </summary>
    public static LabOrder CreateWalkIn(
        string code, Guid patientId, Guid? appointmentId, string? note, IEnumerable<LabOrderItem> items)
    {
        var order = new LabOrder
        {
            Code = code,
            PatientId = patientId,
            AppointmentId = appointmentId,
            EncounterId = null,
            DoctorId = null,
            Note = note,
            Status = LabOrderStatus.Ordered
        };
        order._items.AddRange(items);
        return order;
    }

    /// <summary>Mã phiếu chỉ định duy nhất, ví dụ CLS-000001.</summary>
    public string Code { get; private set; } = null!;

    /// <summary>Phiếu khám nguồn (bác sĩ chỉ định trong lúc khám); null với walk-in.</summary>
    public Guid? EncounterId { get; private set; }

    /// <summary>Lượt tiếp đón gắn kèm (nếu có) — dùng gom hoá đơn theo lượt cho walk-in.</summary>
    public Guid? AppointmentId { get; private set; }

    /// <summary>Bệnh nhân (snapshot từ phiếu khám hoặc do lễ tân chọn khi walk-in).</summary>
    public Guid PatientId { get; private set; }

    /// <summary>Bác sĩ chỉ định (snapshot từ phiếu khám); null với walk-in.</summary>
    public Guid? DoctorId { get; private set; }

    /// <summary>Ghi chú/chỉ định thêm (tuỳ chọn).</summary>
    public string? Note { get; private set; }

    public LabOrderStatus Status { get; private set; }

    /// <summary>
    /// Thời điểm đã lập hoá đơn phí CLS từ phiếu này — null nếu chưa lập.
    /// Cờ chống lập hoá đơn CLS trùng (Mô hình A — như <c>Encounter.MedicationInvoicedAt</c>, ADR 0014 P2/0015).
    /// </summary>
    public DateTimeOffset? InvoicedAt { get; private set; }

    /// <summary>Cụm mục chỉ định (chỉ đọc từ ngoài; thay cả cụm qua <see cref="ReplaceItems"/>).</summary>
    public IReadOnlyCollection<LabOrderItem> Items => _items.AsReadOnly();

    /// <summary>Tổng phí chỉ định = Σ đơn giá các mục (mỗi mục số lượng 1).</summary>
    public decimal TotalAmount => _items.Sum(i => i.UnitPrice);

    /// <summary>Thay toàn bộ cụm mục chỉ định. Chỉ khi chưa <see cref="LabOrderStatus.Completed"/>/<see cref="LabOrderStatus.Cancelled"/>.</summary>
    public Result ReplaceItems(IEnumerable<LabOrderItem> items)
    {
        if (Status is LabOrderStatus.Completed or LabOrderStatus.Cancelled)
            return InvalidTransition(nameof(ReplaceItems));

        _items.Clear();
        _items.AddRange(items);
        return Result.Success();
    }

    /// <summary>Cập nhật ghi chú phiếu.</summary>
    public void UpdateNote(string? note) => Note = note;

    /// <summary>
    /// Nhập kết quả cho một mục. Đặt <c>ResultedAt</c>, chuyển mục sang Completed; nếu mọi mục đã có kết quả
    /// thì phiếu → Completed, ngược lại → InProgress. Sai vòng đời (đã Completed/Cancelled) → 409;
    /// mục không tồn tại → NotFound.
    /// </summary>
    public Result SetItemResult(Guid itemId, string? resultText, string? conclusion, DateTimeOffset when)
    {
        if (Status is LabOrderStatus.Completed or LabOrderStatus.Cancelled)
            return InvalidTransition(nameof(SetItemResult));

        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
            return Result.Failure(Error.NotFound(
                "Paraclinical.ItemNotFound", $"Không tìm thấy mục chỉ định với Id {itemId}."));

        item.SetResult(resultText, conclusion, when);

        Status = _items.All(i => i.Status == LabOrderItemStatus.Completed)
            ? LabOrderStatus.Completed
            : LabOrderStatus.InProgress;
        return Result.Success();
    }

    /// <summary>Huỷ phiếu chỉ định. Chỉ khi chưa Completed/Cancelled.</summary>
    public Result Cancel()
    {
        if (Status is LabOrderStatus.Completed or LabOrderStatus.Cancelled)
            return InvalidTransition(nameof(Cancel));

        Status = LabOrderStatus.Cancelled;
        return Result.Success();
    }

    /// <summary>
    /// Đánh dấu đã lập hoá đơn phí CLS từ phiếu này. Chỉ đặt một lần — đã đặt → lỗi để service map 409
    /// (chống lập hoá đơn CLS trùng, ADR 0015).
    /// </summary>
    public Result MarkInvoiced(DateTimeOffset when)
    {
        if (InvoicedAt is not null)
            return Result.Failure(Error.Conflict(
                "Billing.ParaclinicalAlreadyInvoiced", "Phiếu chỉ định này đã lập hoá đơn phí cận lâm sàng."));

        InvoicedAt = when;
        return Result.Success();
    }

    private Result InvalidTransition(string action) => Result.Failure(Error.Conflict(
        "Paraclinical.InvalidTransition",
        $"Không thể thực hiện '{action}' khi trạng thái phiếu chỉ định là {Status}."));
}
