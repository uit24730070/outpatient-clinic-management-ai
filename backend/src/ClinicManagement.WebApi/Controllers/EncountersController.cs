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
        [FromQuery] DispenseStatus? dispenseStatus = null,
        CancellationToken ct = default)
    {
        var result = await _encounters.GetListAsync(
            new EncounterFilter(page, pageSize, patientId, doctorId, status, dispenseStatus), ct);
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
    /// <b>giữ tồn thuốc</b> (Reserved — kiểm tồn khả dụng, chưa trừ kho thực) — ADR 0021 (PAY-02).
    /// Cấp phát thực chuyển sang <see cref="Dispense"/> sau khi thu tiền; chốt phiếu là việc bác sĩ.
    /// </summary>
    [Authorize(Roles = Roles.RecordEncounter)]
    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
        => ToResponse(await _encounters.CompleteAsync(id, ct));

    /// <summary>
    /// Cấp phát thực đơn thuốc đã thu tiền (Paid → Dispensed): trừ tồn FEFO + ghi sổ cái Dispense — ADR 0021.
    /// Do Dược sĩ (và Admin) thực hiện tại quầy phát thuốc. Chưa thu → 409 <c>Pharmacy.NotPaid</c>.
    /// </summary>
    [Authorize(Roles = Roles.ManagePharmacy)]
    [HttpPost("{id:guid}/dispense")]
    public async Task<IActionResult> Dispense(Guid id, CancellationToken ct)
        => ToResponse(await _encounters.DispenseAsync(id, ct));

    /// <summary>
    /// Hoàn kho đơn thuốc đã cấp phát (Dispensed → Returned): nhập lại tồn đúng lô + ghi sổ cái bù.
    /// Bắt buộc lý do; ghi lại người thực hiện (audit tối thiểu chống hoàn kho nhầm).
    /// </summary>
    [Authorize(Roles = Roles.ManagePharmacy)]
    [HttpPost("{id:guid}/return-stock")]
    public async Task<IActionResult> ReturnStock(Guid id, [FromBody] ReturnStockRequest request, CancellationToken ct)
        => ToResponse(await _encounters.ReturnStockAsync(id, request, CurrentUserId, ct));
}
