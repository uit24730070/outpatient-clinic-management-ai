using ClinicManagement.Application.Specialties;
using ClinicManagement.Application.Specialties.Dtos;
using ClinicManagement.WebApi.Common;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.WebApi.Controllers;

[Route("api/specialties")]
public sealed class SpecialtiesController : ApiControllerBase
{
    private readonly ISpecialtyService _specialties;

    public SpecialtiesController(ISpecialtyService specialties) => _specialties = specialties;

    /// <summary>Tạo chuyên khoa mới.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSpecialtyRequest request, CancellationToken ct)
    {
        var result = await _specialties.CreateAsync(request, ct);
        return ToResponse(result, StatusCodes.Status201Created);
    }

    /// <summary>Lấy danh sách chuyên khoa có phân trang và tìm kiếm.</summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await _specialties.GetListAsync(page, pageSize, search, ct);
        return ToResponse(result);
    }

    /// <summary>Lấy chi tiết một chuyên khoa theo Id.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _specialties.GetByIdAsync(id, ct);
        return ToResponse(result);
    }

    /// <summary>Cập nhật thông tin chuyên khoa.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSpecialtyRequest request, CancellationToken ct)
    {
        var result = await _specialties.UpdateAsync(id, request, ct);
        return ToResponse(result);
    }

    /// <summary>Ngừng sử dụng (xoá mềm) chuyên khoa.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _specialties.DeleteAsync(id, ct);
        return ToResponse(result);
    }
}
