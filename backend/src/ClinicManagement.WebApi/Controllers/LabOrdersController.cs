using ClinicManagement.Application.Paraclinical;
using ClinicManagement.Application.Paraclinical.Dtos;
using ClinicManagement.Domain.Paraclinical;
using ClinicManagement.WebApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.WebApi.Controllers;

/// <summary>
/// Phiếu chỉ định cận lâm sàng. Đọc mở cho mọi vai trò lâm sàng (đã đăng nhập). Chỉ định trong lúc khám =
/// Bác sĩ/Admin (<see cref="Roles.RecordEncounter"/>); đăng ký walk-in = Admin/Lễ tân (<see cref="Roles.ManageStaff"/>);
/// nhập kết quả = Admin/Bác sĩ/Kỹ thuật viên (<see cref="Roles.RecordLabResult"/>). Lập hoá đơn phí CLS ở InvoicesController.
/// </summary>
[Authorize]
[Route("api/lab-orders")]
public sealed class LabOrdersController : ApiControllerBase
{
    private readonly ILabOrderService _labOrders;

    public LabOrdersController(ILabOrderService labOrders) => _labOrders = labOrders;

    /// <summary>Chỉ định cận lâm sàng từ một phiếu khám (snapshot tên/giá dịch vụ Paraclinical).</summary>
    [HttpPost]
    [Authorize(Roles = Roles.RecordEncounter)]
    public async Task<IActionResult> Create([FromBody] CreateLabOrderRequest request, CancellationToken ct)
    {
        var result = await _labOrders.CreateFromEncounterAsync(request, ct);
        return ToResponse(result, StatusCodes.Status201Created);
    }

    /// <summary>Đăng ký cận lâm sàng walk-in (lễ tân): không cần phiếu khám, có thể gắn lượt tiếp đón (ADR 0016).</summary>
    [HttpPost("walk-in")]
    [Authorize(Roles = Roles.ManageStaff)]
    public async Task<IActionResult> CreateWalkIn([FromBody] CreateWalkInLabOrderRequest request, CancellationToken ct)
    {
        var result = await _labOrders.CreateWalkInAsync(request, ct);
        return ToResponse(result, StatusCodes.Status201Created);
    }

    /// <summary>Danh sách phiếu chỉ định có phân trang + lọc theo phiếu khám/bệnh nhân/trạng thái.</summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? encounterId = null,
        [FromQuery] Guid? patientId = null,
        [FromQuery] LabOrderStatus? status = null,
        CancellationToken ct = default)
    {
        var result = await _labOrders.GetListAsync(page, pageSize, encounterId, patientId, status, ct);
        return ToResponse(result);
    }

    /// <summary>Chi tiết một phiếu chỉ định.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _labOrders.GetByIdAsync(id, ct);
        return ToResponse(result);
    }

    /// <summary>Nhập kết quả cho một mục chỉ định (chuyển vòng đời mục/phiếu) — Admin/Bác sĩ/Kỹ thuật viên.</summary>
    [HttpPost("{id:guid}/items/{itemId:guid}/result")]
    [Authorize(Roles = Roles.RecordLabResult)]
    public async Task<IActionResult> SetItemResult(
        Guid id, Guid itemId, [FromBody] SetLabResultRequest request, CancellationToken ct)
    {
        var result = await _labOrders.SetItemResultAsync(id, itemId, request, ct);
        return ToResponse(result);
    }

    /// <summary>Huỷ phiếu chỉ định (chỉ khi chưa Completed/Cancelled).</summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = Roles.RecordEncounter)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var result = await _labOrders.CancelAsync(id, ct);
        return ToResponse(result);
    }
}
