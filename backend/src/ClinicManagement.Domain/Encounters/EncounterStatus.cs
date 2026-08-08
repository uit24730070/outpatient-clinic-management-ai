namespace ClinicManagement.Domain.Encounters;

/// <summary>
/// Trạng thái phiếu khám. Lưu dạng chuỗi (đồng nhất với các enum khác trong hệ thống).
/// Vòng đời: <see cref="Draft"/> → <see cref="Completed"/> (xem ADR 0006).
/// </summary>
public enum EncounterStatus
{
    /// <summary>Đang khám — có thể sửa nội dung và đơn thuốc, lưu nhiều lần.</summary>
    Draft,

    /// <summary>Đã chốt phiếu — khoá nội dung; đồng thời khép lịch khám sang Completed.</summary>
    Completed
}
