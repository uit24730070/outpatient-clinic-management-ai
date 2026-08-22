using ClinicManagement.Application.Encounters;
using ClinicManagement.Application.Encounters.Dtos;
using ClinicManagement.Domain.Encounters;
using ClinicManagement.WebApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.WebApi.Controllers;

[Authorize]
[Route("api/encounters")]
public sealed class EncountersController : ApiControllerBase
{
    private readonly IEncounterService _encounters;

    public EncountersController(IEncounterService encounters) => _encounters = encounters;

    /// <summary>Tạo phiếu khám cho một lịch đang khám (kèm đơn thuốc).</summary>
    [Authorize(Roles = Roles.RecordEncounter)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEncounterRequest request, CancellationToken ct)
    {
        var result = await _encounters.CreateAsync(request, ct);
        return ToResponse(result, StatusCodes.Status201Created);
    }

    /// <summary>Lịch sử khám: danh sách phiếu khám (mới nhất trước), lọc theo bệnh nhân, bác sĩ, trạng thái.</summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? patientId = null,
        [FromQuery] Guid? doctorId = null,
        [FromQuery] EncounterStatus? status = null,
        CancellationToken ct = default)
    {
        var result = await _encounters.GetListAsync(
            new EncounterFilter(page, pageSize, patientId, doctorId, status), ct);
        return ToResponse(result);
    }

    /// <summary>Chi tiết một phiếu khám kèm đơn thuốc.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _encounters.GetByIdAsync(id, ct);
        return ToResponse(result);
    }

    /// <summary>Lấy phiếu khám theo lịch khám (1–1) — tiện mở phiếu từ màn hình lịch.</summary>
    [HttpGet("by-appointment/{appointmentId:guid}")]
    public async Task<IActionResult> GetByAppointment(Guid appointmentId, CancellationToken ct)
    {
        var result = await _encounters.GetByAppointmentAsync(appointmentId, ct);
        return ToResponse(result);
    }

    /// <summary>Sửa nội dung phiếu + thay toàn bộ đơn thuốc (chỉ khi phiếu còn Draft).</summary>
    [Authorize(Roles = Roles.RecordEncounter)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEncounterRequest request, CancellationToken ct)
    {
        var result = await _encounters.UpdateAsync(id, request, ct);
        return ToResponse(result);
    }

    /// <summary>
    /// Chốt phiếu: Draft → Completed, đồng thời khép lịch khám (InProgress → Completed) và
    /// <b>cấp phát thuốc theo đơn</b> (trừ tồn FEFO, ghi sổ cái Dispense) — ADR 0011.
    /// Vì gộp cấp phát vào bước này, cho phép cả ba vai trò thực hiện (Admin/Lễ tân/Bác sĩ).
    /// </summary>
    [Authorize(Roles = Roles.DispenseEncounter)]
    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
        => ToResponse(await _encounters.CompleteAsync(id, ct));
}
