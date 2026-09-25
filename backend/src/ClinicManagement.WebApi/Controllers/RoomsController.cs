using ClinicManagement.Application.Rooms;
using ClinicManagement.Application.Rooms.Dtos;
using ClinicManagement.WebApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.WebApi.Controllers;

[Authorize]
[Route("api/rooms")]
public sealed class RoomsController : ApiControllerBase
{
    private readonly IRoomService _rooms;

    public RoomsController(IRoomService rooms) => _rooms = rooms;

    /// <summary>Tạo phòng khám mới.</summary>
    [Authorize(Roles = Roles.ManageStaff)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoomRequest request, CancellationToken ct)
    {
        var result = await _rooms.CreateAsync(request, ct);
        return ToResponse(result, StatusCodes.Status201Created);
    }

    /// <summary>Danh sách phòng khám có phân trang + tìm kiếm (theo tên/mã).</summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false,
        CancellationToken ct = default)
    {
        var result = await _rooms.GetListAsync(page, pageSize, search, sortBy, sortDesc, ct);
        return ToResponse(result);
    }

    /// <summary>Chi tiết một phòng khám.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _rooms.GetByIdAsync(id, ct);
        return ToResponse(result);
    }

    /// <summary>Cập nhật thông tin phòng khám.</summary>
    [Authorize(Roles = Roles.ManageStaff)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoomRequest request, CancellationToken ct)
    {
        var result = await _rooms.UpdateAsync(id, request, ct);
        return ToResponse(result);
    }

    /// <summary>Ngừng sử dụng (xoá mềm) phòng khám.</summary>
    [Authorize(Roles = Roles.ManageStaff)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _rooms.DeleteAsync(id, ct);
        return ToResponse(result);
    }
}
