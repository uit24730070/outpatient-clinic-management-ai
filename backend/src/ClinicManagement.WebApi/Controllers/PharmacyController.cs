using ClinicManagement.Application.Pharmacy;
using ClinicManagement.WebApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.WebApi.Controllers;

[Authorize(Roles = Roles.ManagePharmacy)]
[Route("api/pharmacy")]
public sealed class PharmacyController : ApiControllerBase
{
    private readonly IPharmacyAlertService _alerts;

    public PharmacyController(IPharmacyAlertService alerts) => _alerts = alerts;

    /// <summary>Cảnh báo kho: thuốc tồn thấp (≤ ngưỡng đặt lại) và lô sắp/đã hết hạn (trong N ngày tới).</summary>
    [HttpGet("alerts")]
    public async Task<IActionResult> GetAlerts([FromQuery] int expiringInDays = 30, CancellationToken ct = default)
        => ToResponse(await _alerts.GetAlertsAsync(expiringInDays, ct));
}
