namespace ClinicManagement.Domain.Visits;

/// <summary>
/// Trạng thái một lượt tiếp nhận (Visit). Vòng đời nhẹ: <see cref="Open"/> khi bệnh nhân đến
/// và còn dịch vụ đang xử lý → <see cref="Closed"/> khi hoàn tất (khám xong + thu tiền), hoặc
/// <see cref="Cancelled"/> nếu huỷ lượt. Lưu dạng chuỗi (đồng nhất các enum khác).
/// </summary>
public enum VisitStatus
{
    /// <summary>Đang mở — bệnh nhân đã tiếp nhận, còn dịch vụ trong lượt.</summary>
    Open,

    /// <summary>Đã đóng — lượt khám hoàn tất.</summary>
    Closed,

    /// <summary>Đã huỷ lượt.</summary>
    Cancelled
}
