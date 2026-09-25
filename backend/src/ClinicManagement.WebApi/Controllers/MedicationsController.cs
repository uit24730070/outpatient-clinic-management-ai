using ClinicManagement.Application.Medications;
using ClinicManagement.Application.Medications.Dtos;
using ClinicManagement.WebApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.WebApi.Controllers;

[Authorize]
[Route("api/medications")]
public sealed class MedicationsController : ApiControllerBase
{
    private readonly IMedicationService _medications;

    public MedicationsController(IMedicationService medications) => _medications = medications;

    /// <summary>Tạo thuốc mới trong danh mục.</summary>
    [Authorize(Roles = Roles.ManagePharmacy)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMedicationRequest request, CancellationToken ct)
    {
        var result = await _medications.CreateAsync(request, ct);
        return ToResponse(result, StatusCodes.Status201Created);
    }

    /// <summary>Danh sách thuốc có phân trang + tìm kiếm (theo tên/mã/hoạt chất), kèm tồn tổng.</summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false,
        CancellationToken ct = default)
    {
        var result = await _medications.GetListAsync(page, pageSize, search, sortBy, sortDesc, ct);
        return ToResponse(result);
    }

    /// <summary>Chi tiết một thuốc kèm tồn tổng.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _medications.GetByIdAsync(id, ct);
        return ToResponse(result);
    }

    /// <summary>Danh sách lô của một thuốc (tồn theo lô + hạn dùng).</summary>
    [HttpGet("{id:guid}/batches")]
    public async Task<IActionResult> GetBatches(Guid id, CancellationToken ct)
    {
        var result = await _medications.GetBatchesAsync(id, ct);
        return ToResponse(result);
    }

    /// <summary>Cập nhật thông tin thuốc.</summary>
    [Authorize(Roles = Roles.ManagePharmacy)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMedicationRequest request, CancellationToken ct)
    {
        var result = await _medications.UpdateAsync(id, request, ct);
        return ToResponse(result);
    }

    /// <summary>Ngừng sử dụng (xoá mềm) thuốc.</summary>
    [Authorize(Roles = Roles.ManagePharmacy)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _medications.DeleteAsync(id, ct);
        return ToResponse(result);
    }
}
