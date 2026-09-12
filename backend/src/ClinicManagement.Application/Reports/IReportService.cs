using ClinicManagement.Application.Reports.Dtos;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Application.Reports;

/// <summary>
/// Dịch vụ báo cáo/thống kê vận hành (Epic 7 — Reporting, ADR 0020). Chỉ-đọc, tổng hợp phía server
/// từ các bảng hiện có (Count/Sum/GroupBy), tôn trọng soft-delete + múi giờ phòng khám (UTC+7).
/// </summary>
public interface IReportService
{
    /// <summary>R-01 · Chỉ số tổng quan (KPI) cho hôm nay + tổng số hiện hành.</summary>
    Task<Result<OverviewDto>> GetOverviewAsync(CancellationToken ct = default);

    /// <summary>R-02 · Báo cáo lịch khám theo khoảng ngày (mặc định 7 ngày gần nhất), lọc tuỳ chọn theo bác sĩ.</summary>
    Task<Result<AppointmentReportDto>> GetAppointmentReportAsync(
        DateOnly? from, DateOnly? to, Guid? doctorId, CancellationToken ct = default);

    /// <summary>
    /// R-03 · Năng suất theo bác sĩ trong khoảng ngày. Nếu <paramref name="restrictUserId"/> có giá trị
    /// (người gọi là Bác sĩ) thì ép chỉ lấy dữ liệu bác sĩ gắn với tài khoản đó (bỏ qua <paramref name="doctorId"/>).
    /// </summary>
    Task<Result<IReadOnlyList<DoctorProductivityDto>>> GetDoctorProductivityAsync(
        DateOnly? from, DateOnly? to, Guid? doctorId, Guid? restrictUserId, CancellationToken ct = default);

    /// <summary>R-04 · Báo cáo doanh thu theo khoảng ngày (chỉ hoá đơn đã thanh toán), tách theo loại khoản mục.</summary>
    Task<Result<RevenueReportDto>> GetRevenueReportAsync(
        DateOnly? from, DateOnly? to, CancellationToken ct = default);
}
