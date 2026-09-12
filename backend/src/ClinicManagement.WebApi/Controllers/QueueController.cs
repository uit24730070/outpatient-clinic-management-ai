using ClinicManagement.Application.Queue;
using ClinicManagement.Application.Queue.Dtos;
using ClinicManagement.Domain.Queue;
using ClinicManagement.WebApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.WebApi.Controllers;

/// <summary>
/// Hàng đợi khám / số thứ tự trong ngày (ADR 0019). Điều phối = Lễ tân/Điều dưỡng/Admin
/// (<see cref="Roles.ManageQueue"/>); đọc mở cho mọi vai trò đã đăng nhập.
/// </summary>
[Authorize]
[Route("api/queue")]
public sealed class QueueController : ApiControllerBase
{
    private readonly IQueueService _queue;

    public QueueController(IQueueService queue) => _queue = queue;

    /// <summary>Lấy số hàng đợi mới (hỗ trợ khách vãng lai không lịch).</summary>
    [Authorize(Roles = Roles.ManageQueue)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateQueueTicketRequest request, CancellationToken ct)
    {
        var result = await _queue.CreateAsync(request, ct);
        return ToResponse(result, StatusCodes.Status201Created);
    }

    /// <summary>Danh sách vé hàng đợi theo ngày/phòng/bác sĩ/trạng thái (mặc định hôm nay).</summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] DateOnly? date = null,
        [FromQuery] Guid? roomId = null,
        [FromQuery] Guid? doctorId = null,
        [FromQuery] QueueTicketStatus? status = null,
        CancellationToken ct = default)
    {
        var result = await _queue.GetListAsync(new QueueFilter(date, roomId, doctorId, status), ct);
        return ToResponse(result);
    }

    /// <summary>Gán/đổi phòng &amp; bác sĩ cho vé (điều phối).</summary>
    [Authorize(Roles = Roles.ManageQueue)]
    [HttpPost("{id:guid}/assign")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignQueueTicketRequest request, CancellationToken ct)
        => ToResponse(await _queue.AssignAsync(id, request, ct));

    /// <summary>Gọi số: Waiting → Called.</summary>
    [Authorize(Roles = Roles.ManageQueue)]
    [HttpPost("{id:guid}/call")]
    public async Task<IActionResult> Call(Guid id, CancellationToken ct)
        => ToResponse(await _queue.CallAsync(id, ct));

    /// <summary>Bắt đầu khám: Called → InProgress.</summary>
    [Authorize(Roles = Roles.ManageQueue)]
    [HttpPost("{id:guid}/start")]
    public async Task<IActionResult> Start(Guid id, CancellationToken ct)
        => ToResponse(await _queue.StartAsync(id, ct));

    /// <summary>Hoàn tất: InProgress → Done.</summary>
    [Authorize(Roles = Roles.ManageQueue)]
    [HttpPost("{id:guid}/done")]
    public async Task<IActionResult> Done(Guid id, CancellationToken ct)
        => ToResponse(await _queue.DoneAsync(id, ct));

    /// <summary>Bỏ qua: Waiting/Called → Skipped.</summary>
    [Authorize(Roles = Roles.ManageQueue)]
    [HttpPost("{id:guid}/skip")]
    public async Task<IActionResult> Skip(Guid id, CancellationToken ct)
        => ToResponse(await _queue.SkipAsync(id, ct));
}
