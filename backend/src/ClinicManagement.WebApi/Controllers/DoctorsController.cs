using ClinicManagement.Application.Doctors;
using ClinicManagement.Application.Doctors.Dtos;
using ClinicManagement.WebApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.WebApi.Controllers;

[Authorize]
[Route("api/doctors")]
public sealed class DoctorsController : ApiControllerBase
{
    private readonly IDoctorService _doctors;

    public DoctorsController(IDoctorService doctors) => _doctors = doctors;

    /// <summary>Tạo hồ sơ bác sĩ mới.</summary>
    [Authorize(Roles = Roles.ManageStaff)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDoctorRequest request, CancellationToken ct)
    {
        var result = await _doctors.CreateAsync(request, ct);
        return ToResponse(result, StatusCodes.Status201Created);
    }

    /// <summary>Lấy danh sách bác sĩ có phân trang và tìm kiếm.</summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await _doctors.GetListAsync(page, pageSize, search, ct);
        return ToResponse(result);
    }

    /// <summary>Lấy chi tiết một bác sĩ theo Id.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _doctors.GetByIdAsync(id, ct);
        return ToResponse(result);
    }

    /// <summary>Cập nhật thông tin bác sĩ.</summary>
    [Authorize(Roles = Roles.ManageStaff)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDoctorRequest request, CancellationToken ct)
    {
        var result = await _doctors.UpdateAsync(id, request, ct);
        return ToResponse(result);
    }

    /// <summary>Ngừng sử dụng (xoá mềm) hồ sơ bác sĩ.</summary>
    [Authorize(Roles = Roles.ManageStaff)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _doctors.DeleteAsync(id, ct);
        return ToResponse(result);
    }
}
