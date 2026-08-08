namespace ClinicManagement.Domain.Appointments;

/// <summary>
/// Trạng thái vòng đời lịch khám. Lưu dưới dạng chuỗi. Xem ADR 0005 (máy trạng thái).
/// <c>Cancelled</c> là trạng thái nghiệp vụ (còn hiển thị), khác xoá mềm.
/// </summary>
public enum AppointmentStatus
{
    /// <summary>Đã đặt lịch, chờ bệnh nhân đến.</summary>
    Scheduled = 0,

    /// <summary>Bệnh nhân đã đến (check-in), đang chờ khám.</summary>
    CheckedIn = 1,

    /// <summary>Đang khám.</summary>
    InProgress = 2,

    /// <summary>Đã khám xong.</summary>
    Completed = 3,

    /// <summary>Đã huỷ lịch.</summary>
    Cancelled = 4,

    /// <summary>Bệnh nhân không đến.</summary>
    NoShow = 5
}
