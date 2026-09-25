using ClinicManagement.Application.Visits;
using ClinicManagement.Application.Visits.Dtos;
using ClinicManagement.Domain.Visits;
using ClinicManagement.WebApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.WebApi.Controllers;

[Authorize]
[Route("api/visits")]
public sealed class VisitsController : ApiControllerBase
{
    private readonly IVisitService _visits;

    public VisitsController(IVisitService visits) => _visits = visits;

    /// <summary>Tạo lượt tiếp nhận kèm 1..n dịch vụ khám (walk-in — ADR 0017).</summary>
    [Authorize(Roles = Roles.ManageStaff)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVisitRequest request, CancellationToken ct)
        => ToResponse(await _visits.CreateAsync(request, ct), StatusCodes.Status201Created);

    /// <summary>Danh sách lượt tiếp nhận, lọc theo ngày, bệnh nhân, trạng thái.</summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? patientId = null,
        [FromQuery] VisitStatus? status = null,
        [FromQuery] DateOnly? date = null,
        CancellationToken ct = default)
        => ToResponse(await _visits.GetListAsync(page, pageSize, patientId, status, date, ct));

    /// <summary>Chi tiết một lượt tiếp nhận (các dịch vụ khám + tổng viện phí gom cả lượt).</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => ToResponse(await _visits.GetByIdAsync(id, ct));

    /// <summary>Thêm một dịch vụ khám vào lượt đang mở.</summary>
    [Authorize(Roles = Roles.ManageStaff)]
    [HttpPost("{id:guid}/services")]
    public async Task<IActionResult> AddService(Guid id, [FromBody] AddVisitServiceRequest request, CancellationToken ct)
        => ToResponse(await _visits.AddServiceAsync(id, request, ct));

    /// <summary>Đóng lượt: Open → Closed.</summary>
    [Authorize(Roles = Roles.ManageStaff)]
    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, CancellationToken ct)
        => ToResponse(await _visits.CloseAsync(id, ct));

    /// <summary>Huỷ lượt: Open → Cancelled.</summary>
    [Authorize(Roles = Roles.ManageStaff)]
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
        => ToResponse(await _visits.CancelAsync(id, ct));

    /// <summary>Mở lại lượt đã đóng: Closed → Open (vd Lễ tân cần thêm dịch vụ khám/CLS).</summary>
    [Authorize(Roles = Roles.ManageStaff)]
    [HttpPost("{id:guid}/reopen")]
    public async Task<IActionResult> Reopen(Guid id, CancellationToken ct)
        => ToResponse(await _visits.ReopenAsync(id, ct));
}
