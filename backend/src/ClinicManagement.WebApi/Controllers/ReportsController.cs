using ClinicManagement.Application.Reports;
using ClinicManagement.WebApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.WebApi.Controllers;

/// <summary>
/// Báo cáo & thống kê vận hành (Epic 7 — Reporting, ADR 0020). Tất cả chỉ-đọc, tổng hợp phía server.
/// Overview/lịch/doanh thu dành cho quản lý (<see cref="Roles.ManageStaff"/> = Admin + Lễ tân);
/// năng suất bác sĩ mở thêm cho Bác sĩ nhưng ép chỉ xem của mình.
/// </summary>
[Authorize]
[Route("api/reports")]
public sealed class ReportsController : ApiControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports) => _reports = reports;

    /// <summary>R-01 · Chỉ số tổng quan (KPI) cho hôm nay.</summary>
    [Authorize(Roles = Roles.ManageStaff)]
    [HttpGet("overview")]
    public async Task<IActionResult> Overview(CancellationToken ct)
        => ToResponse(await _reports.GetOverviewAsync(ct));

    /// <summary>R-02 · Báo cáo lịch khám theo khoảng ngày (mặc định 7 ngày gần nhất).</summary>
    [Authorize(Roles = Roles.ManageStaff)]
    [HttpGet("appointments")]
    public async Task<IActionResult> Appointments(
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] Guid? doctorId = null,
        CancellationToken ct = default)
        => ToResponse(await _reports.GetAppointmentReportAsync(from, to, doctorId, ct));

    /// <summary>R-03 · Năng suất theo bác sĩ. Bác sĩ đăng nhập chỉ thấy dữ liệu của mình.</summary>
    [Authorize(Roles = Roles.ManageStaff + "," + Roles.Doctor)]
    [HttpGet("by-doctor")]
    public async Task<IActionResult> ByDoctor(
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        CancellationToken ct = default)
    {
        var restrictUserId = CurrentUserRole == Roles.Doctor ? CurrentUserId : (Guid?)null;
        return ToResponse(await _reports.GetDoctorProductivityAsync(from, to, null, restrictUserId, ct));
    }

    /// <summary>R-04 · Báo cáo doanh thu theo khoảng ngày (chỉ hoá đơn đã thanh toán).</summary>
    [Authorize(Roles = Roles.ManageStaff)]
    [HttpGet("revenue")]
    public async Task<IActionResult> Revenue(
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        CancellationToken ct = default)
        => ToResponse(await _reports.GetRevenueReportAsync(from, to, ct));
}
