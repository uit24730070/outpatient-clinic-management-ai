using ClinicManagement.Application.Appointments;
using ClinicManagement.Application.Appointments.Dtos;
using ClinicManagement.Domain.Appointments;
using ClinicManagement.WebApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.WebApi.Controllers;

[Authorize]
[Route("api/appointments")]
public sealed class AppointmentsController : ApiControllerBase
{
    private readonly IAppointmentService _appointments;

    public AppointmentsController(IAppointmentService appointments) => _appointments = appointments;

    /// <summary>Đặt lịch khám mới.</summary>
    [Authorize(Roles = Roles.ManageStaff)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentRequest request, CancellationToken ct)
    {
        var result = await _appointments.CreateAsync(request, ct);
        return ToResponse(result, StatusCodes.Status201Created);
    }

    /// <summary>Danh sách lịch khám / hàng đợi trong ngày, lọc theo ngày, bác sĩ, bệnh nhân, trạng thái.</summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateOnly? date = null,
        [FromQuery] Guid? doctorId = null,
        [FromQuery] Guid? patientId = null,
        [FromQuery] AppointmentStatus? status = null,
        CancellationToken ct = default)
    {
        var result = await _appointments.GetListAsync(
            new AppointmentFilter(page, pageSize, date, doctorId, patientId, status), ct);
        return ToResponse(result);
    }

    /// <summary>Chi tiết một lịch khám theo Id.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _appointments.GetByIdAsync(id, ct);
        return ToResponse(result);
    }

    /// <summary>Đổi khung giờ/lý do của lịch khám.</summary>
    [Authorize(Roles = Roles.ManageStaff)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAppointmentRequest request, CancellationToken ct)
    {
        var result = await _appointments.UpdateAsync(id, request, ct);
        return ToResponse(result);
    }

    /// <summary>Xoá mềm lịch khám (khác với huỷ — xem ADR 0005).</summary>
    [Authorize(Roles = Roles.ManageStaff)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _appointments.DeleteAsync(id, ct);
        return ToResponse(result);
    }

    /// <summary>Check-in: Scheduled → CheckedIn.</summary>
    [Authorize(Roles = Roles.ManageStaff)]
    [HttpPost("{id:guid}/check-in")]
    public async Task<IActionResult> CheckIn(Guid id, CancellationToken ct)
        => ToResponse(await _appointments.CheckInAsync(id, ct));

    /// <summary>Bắt đầu khám: CheckedIn → InProgress.</summary>
    [Authorize(Roles = Roles.ManageStaff)]
    [HttpPost("{id:guid}/start")]
    public async Task<IActionResult> Start(Guid id, CancellationToken ct)
        => ToResponse(await _appointments.StartAsync(id, ct));

    /// <summary>Hoàn tất khám: InProgress → Completed.</summary>
    [Authorize(Roles = Roles.ManageStaff)]
    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
        => ToResponse(await _appointments.CompleteAsync(id, ct));

    /// <summary>Huỷ lịch: Scheduled/CheckedIn/InProgress → Cancelled.</summary>
    [Authorize(Roles = Roles.ManageStaff)]
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
        => ToResponse(await _appointments.CancelAsync(id, ct));

    /// <summary>Đánh dấu không đến: Scheduled/CheckedIn → NoShow.</summary>
    [Authorize(Roles = Roles.ManageStaff)]
    [HttpPost("{id:guid}/no-show")]
    public async Task<IActionResult> NoShow(Guid id, CancellationToken ct)
        => ToResponse(await _appointments.MarkNoShowAsync(id, ct));
}
