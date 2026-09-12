using ClinicManagement.Application.Vitals;
using ClinicManagement.Application.Vitals.Dtos;
using ClinicManagement.WebApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.WebApi.Controllers;

/// <summary>
/// Sinh hiệu gắn lượt khám (ADR 0019). Ghi = Điều dưỡng/Admin (<see cref="Roles.RecordVitals"/>);
/// đọc mở cho mọi vai trò lâm sàng (bác sĩ xem trong bệnh án).
/// </summary>
[Authorize]
[Route("api/appointments/{appointmentId:guid}/vitals")]
public sealed class VitalsController : ApiControllerBase
{
    private readonly IVitalsService _vitals;

    public VitalsController(IVitalsService vitals) => _vitals = vitals;

    /// <summary>Nhập/cập nhật (upsert) sinh hiệu cho lượt khám.</summary>
    [Authorize(Roles = Roles.RecordVitals)]
    [HttpPost]
    public async Task<IActionResult> Upsert(
        Guid appointmentId, [FromBody] UpsertVitalsRequest request, CancellationToken ct)
    {
        var result = await _vitals.UpsertAsync(appointmentId, request, CurrentUserId, ct);
        return ToResponse(result);
    }

    /// <summary>Lấy sinh hiệu của lượt khám; data=null nếu chưa đo.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(Guid appointmentId, CancellationToken ct)
    {
        var result = await _vitals.GetByAppointmentAsync(appointmentId, ct);
        return ToResponse(result);
    }
}
