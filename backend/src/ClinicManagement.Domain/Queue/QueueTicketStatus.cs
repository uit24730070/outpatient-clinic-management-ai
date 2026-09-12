namespace ClinicManagement.Domain.Queue;

/// <summary>
/// Trạng thái vé hàng đợi khám (ADR 0019). Lưu dưới dạng chuỗi. Đây là trạng thái <b>hàng đợi</b>,
/// phản ánh best-effort tiến trình gọi khám — <b>không</b> thay <c>Appointment.Status</c> (nguồn sự thật
/// lâm sàng, ADR 0005).
/// </summary>
public enum QueueTicketStatus
{
    /// <summary>Đang chờ gọi.</summary>
    Waiting = 0,

    /// <summary>Đã gọi số (mời vào phòng).</summary>
    Called = 1,

    /// <summary>Đang khám.</summary>
    InProgress = 2,

    /// <summary>Đã khám xong / rời hàng đợi.</summary>
    Done = 3,

    /// <summary>Bỏ qua (không đáp/không đến khi gọi).</summary>
    Skipped = 4
}
