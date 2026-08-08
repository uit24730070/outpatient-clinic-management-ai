using ClinicManagement.Application.Ai;
using ClinicManagement.Application.Patients;
using ClinicManagement.Application.Patients.Dtos;
using ClinicManagement.WebApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.WebApi.Controllers;

[Authorize]
[Route("api/patients")]
public sealed class PatientsController : ApiControllerBase
{
    private readonly IPatientService _patients;
    private readonly IPatientSummaryService _summaries;

    public PatientsController(IPatientService patients, IPatientSummaryService summaries)
    {
        _patients = patients;
        _summaries = summaries;
    }

    /// <summary>Tạo hồ sơ bệnh nhân mới.</summary>
    [Authorize(Roles = Roles.ManageStaff)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePatientRequest request, CancellationToken ct)
    {
        var result = await _patients.CreateAsync(request, ct);
        return ToResponse(result, StatusCodes.Status201Created);
    }

    /// <summary>Lấy danh sách bệnh nhân có phân trang và tìm kiếm.</summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await _patients.GetListAsync(page, pageSize, search, ct);
        return ToResponse(result);
    }

    /// <summary>Lấy chi tiết một bệnh nhân theo Id.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _patients.GetByIdAsync(id, ct);
        return ToResponse(result);
    }

    /// <summary>Cập nhật thông tin bệnh nhân.</summary>
    [Authorize(Roles = Roles.ManageStaff)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePatientRequest request, CancellationToken ct)
    {
        var result = await _patients.UpdateAsync(id, request, ct);
        return ToResponse(result);
    }

    /// <summary>Ngừng sử dụng (xoá mềm) hồ sơ bệnh nhân.</summary>
    [Authorize(Roles = Roles.ManageStaff)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _patients.DeleteAsync(id, ct);
        return ToResponse(result);
    }

    /// <summary>Tóm tắt lịch sử khám của bệnh nhân bằng AI (đọc bệnh án — chỉ Bác sĩ/Admin).</summary>
    [Authorize(Roles = Roles.RecordEncounter)]
    [HttpPost("{id:guid}/ai-summary")]
    public async Task<IActionResult> AiSummary(Guid id, CancellationToken ct)
    {
        var result = await _summaries.SummarizeAsync(id, ct);
        return ToResponse(result);
    }
}
