namespace ClinicManagement.Application.Reports.Dtos;

/// <summary>
/// Phân rã số lịch khám theo trạng thái (khớp <see cref="ClinicManagement.Domain.Appointments.AppointmentStatus"/>).
/// Dùng field tường minh thay cho dictionary theo enum vì enum serialize dạng số trong hệ thống (xem ghi chú dự án).
/// </summary>
public sealed record AppointmentStatusBreakdown(
    int Scheduled,
    int CheckedIn,
    int InProgress,
    int Completed,
    int Cancelled,
    int NoShow);

/// <summary>
/// R-01 · Chỉ số tổng quan cho dashboard quản lý. Tất cả tổng hợp phía server, tôn trọng soft-delete.
/// "Hôm nay" theo múi giờ phòng khám (UTC+7).
/// </summary>
public sealed record OverviewDto(
    // Tổng bệnh nhân đang hoạt động (chưa xoá mềm).
    int TotalActivePatients,
    // Tổng bác sĩ đang hoạt động.
    int TotalDoctors,
    // Số phiếu khám lập hôm nay.
    int EncountersToday,
    // Doanh thu hôm nay (tổng hoá đơn đã thanh toán).
    decimal RevenueToday,
    // Số vé đang chờ trong hàng đợi hôm nay.
    int QueueWaiting,
    // Tổng lịch khám hôm nay.
    int AppointmentsToday,
    // Phân rã lịch hôm nay theo trạng thái.
    AppointmentStatusBreakdown AppointmentsByStatus);

/// <summary>Một ngày trong báo cáo lịch: tổng + phân rã theo trạng thái.</summary>
public sealed record AppointmentDayReport(
    DateOnly Date,
    int Total,
    AppointmentStatusBreakdown ByStatus);

/// <summary>R-02 · Báo cáo lịch khám theo khoảng ngày (mỗi ngày một dòng, đủ ngày trong khoảng kể cả 0).</summary>
public sealed record AppointmentReportDto(
    DateOnly From,
    DateOnly To,
    int Total,
    IReadOnlyList<AppointmentDayReport> Days);

/// <summary>R-03 · Năng suất một bác sĩ trong khoảng ngày.</summary>
public sealed record DoctorProductivityDto(
    Guid DoctorId,
    string DoctorCode,
    string DoctorName,
    // Tổng lịch khám được phân cho bác sĩ.
    int TotalAppointments,
    // Số lịch đã hoàn tất.
    int CompletedAppointments,
    // Số phiếu khám bác sĩ đã lập.
    int Encounters);

/// <summary>Doanh thu một ngày, tách theo loại khoản mục hoá đơn.</summary>
public sealed record RevenueDayReport(
    DateOnly Date,
    decimal Total,
    decimal ServiceFee,
    decimal Medication,
    decimal Paraclinical,
    decimal Other);

/// <summary>R-04 · Báo cáo doanh thu theo khoảng ngày (chỉ hoá đơn đã thanh toán).</summary>
public sealed record RevenueReportDto(
    DateOnly From,
    DateOnly To,
    decimal GrandTotal,
    decimal ServiceFeeTotal,
    decimal MedicationTotal,
    decimal ParaclinicalTotal,
    decimal OtherTotal,
    IReadOnlyList<RevenueDayReport> Days);
