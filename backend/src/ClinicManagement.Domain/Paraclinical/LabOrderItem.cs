namespace ClinicManagement.Domain.Paraclinical;

/// <summary>
/// Một mục của phiếu chỉ định cận lâm sàng (<see cref="LabOrder"/>) — tương ứng một dịch vụ CLS.
/// Là <b>owned entity</b> (không có vòng đời độc lập; tạo/đọc theo cả cụm cùng phiếu, như
/// <c>InvoiceItem</c>/<c>PrescriptionItem</c>). Có <see cref="Id"/> riêng (Guid) để địa chỉ hoá khi nhập
/// kết quả từng mục. <see cref="UnitPrice"/> được <b>snapshot</b> từ bảng giá tại thời điểm chỉ định
/// (đổi giá sau không ảnh hưởng phiếu cũ — ADR 0015).
/// </summary>
public class LabOrderItem
{
    private readonly List<LabResultParameter> _parameters = new();

    // EF Core cần constructor không tham số.
    private LabOrderItem() { }

    public LabOrderItem(Guid servicePriceId, string serviceName, decimal unitPrice)
    {
        Id = Guid.NewGuid();
        ServicePriceId = servicePriceId;
        ServiceName = serviceName;
        UnitPrice = unitPrice;
        Status = LabOrderItemStatus.Pending;
    }

    /// <summary>Định danh mục (để nhập kết quả theo mục qua API).</summary>
    public Guid Id { get; private set; }

    /// <summary>Dịch vụ CLS được chỉ định (tham chiếu bảng giá — chỉ để tra soát).</summary>
    public Guid ServicePriceId { get; private set; }

    /// <summary>Tên dịch vụ (snapshot lúc chỉ định).</summary>
    public string ServiceName { get; private set; } = null!;

    /// <summary>Đơn giá (VND) — snapshot lúc chỉ định.</summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>Kết quả (văn bản tự do; null khi chưa có).</summary>
    public string? ResultText { get; private set; }

    /// <summary>Kết luận/nhận định (tuỳ chọn).</summary>
    public string? Conclusion { get; private set; }

    /// <summary>
    /// Kết quả có cấu trúc theo từng thông số (ADR 0025) — dùng cho mục nhóm Xét nghiệm thay cho
    /// <see cref="ResultText"/>; rỗng với mục dùng văn bản tự do (Chẩn đoán hình ảnh/Thăm dò chức
    /// năng/Nội soi). Hai cách không loại trừ nhau — mục có thể có cả hai nếu kỹ thuật viên muốn.
    /// </summary>
    public IReadOnlyCollection<LabResultParameter> Parameters => _parameters.AsReadOnly();

    /// <summary>Trạng thái mục (chờ/đã có kết quả).</summary>
    public LabOrderItemStatus Status { get; private set; }

    /// <summary>Thời điểm nhập kết quả (null khi chưa có).</summary>
    public DateTimeOffset? ResultedAt { get; private set; }

    /// <summary>
    /// Ghi kết quả cho mục và đánh dấu hoàn tất. Gọi từ <see cref="LabOrder.SetItemResult"/>.
    /// <paramref name="parameters"/> null/rỗng → xoá hết thông số cũ (thay toàn bộ, không gộp).
    /// </summary>
    internal void SetResult(
        string? resultText, string? conclusion, IEnumerable<LabResultParameter>? parameters, DateTimeOffset when)
    {
        ResultText = resultText;
        Conclusion = conclusion;
        _parameters.Clear();
        if (parameters is not null)
            _parameters.AddRange(parameters);
        Status = LabOrderItemStatus.Completed;
        ResultedAt = when;
    }
}
