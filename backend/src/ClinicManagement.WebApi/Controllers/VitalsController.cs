using ClinicManagement.Application.Vitals;
using ClinicManagement.Application.Vitals.Dtos;
using ClinicManagement.WebApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.WebApi.Controllers;

/// <summary>
/// Sinh hiệu gắn lượt khám (ADR 0019) — mỗi lần đo là một bản ghi lịch sử (không upsert, có thể đo lại
/// nhiều lần). Ghi = Điều dưỡng/Admin (<see cref="Roles.RecordVitals"/>); đọc mở cho mọi vai trò lâm sàng
/// (bác sĩ xem trong bệnh án).
/// </summary>
[Authorize]
[Route("api/appointments/{appointmentId:guid}/vitals")]
public sealed class VitalsController : ApiControllerBase
{
    private readonly IVitalsService _vitals;

    public VitalsController(IVitalsService vitals) => _vitals = vitals;

    /// <summary>Ghi một lần đo sinh hiệu mới cho lịch khám (luôn tạo bản ghi mới).</summary>
    [Authorize(Roles = Roles.RecordVitals)]
    [HttpPost]
    public async Task<IActionResult> Record(
        Guid appointmentId, [FromBody] RecordVitalsRequest request, CancellationToken ct)
    {
        var result = await _vitals.RecordAsync(appointmentId, request, CurrentUserId, ct);
        return ToResponse(result, StatusCodes.Status201Created);
    }

    /// <summary>Lần đo sinh hiệu gần nhất của lịch khám; data=null nếu chưa đo.</summary>
    [HttpGet]
    public async Task<IActionResult> GetLatest(Guid appointmentId, CancellationToken ct)
    {
        var result = await _vitals.GetLatestByAppointmentAsync(appointmentId, ct);
        return ToResponse(result);
    }

    /// <summary>Toàn bộ lịch sử đo sinh hiệu của lịch khám (mới nhất trước).</summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(Guid appointmentId, CancellationToken ct)
    {
        var result = await _vitals.GetHistoryByAppointmentAsync(appointmentId, ct);
        return ToResponse(result);
    }
}
