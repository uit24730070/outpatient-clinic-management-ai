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
    private readonly IDoctorScheduleService _schedules;

    public DoctorsController(IDoctorService doctors, IDoctorScheduleService schedules)
    {
        _doctors = doctors;
        _schedules = schedules;
    }

    /// <summary>Tạo hồ sơ bác sĩ mới.</summary>
    [Authorize(Roles = Roles.ManageCatalog)]
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
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false,
        CancellationToken ct = default)
    {
        var result = await _doctors.GetListAsync(page, pageSize, search, sortBy, sortDesc, ct);
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
    [Authorize(Roles = Roles.ManageCatalog)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDoctorRequest request, CancellationToken ct)
    {
        var result = await _doctors.UpdateAsync(id, request, ct);
        return ToResponse(result);
    }

    /// <summary>Ngừng sử dụng (xoá mềm) hồ sơ bác sĩ.</summary>
    [Authorize(Roles = Roles.ManageCatalog)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _doctors.DeleteAsync(id, ct);
        return ToResponse(result);
    }

    /// <summary>Gắn một tài khoản (role Bác sĩ) vào hồ sơ bác sĩ (chỉ Admin).</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpPost("{id:guid}/link-user")]
    public async Task<IActionResult> LinkUser(Guid id, [FromBody] LinkUserRequest request, CancellationToken ct)
    {
        var result = await _doctors.LinkUserAsync(id, request, ct);
        return ToResponse(result);
    }

    /// <summary>Gỡ liên kết tài khoản khỏi hồ sơ bác sĩ (chỉ Admin).</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpPost("{id:guid}/unlink-user")]
    public async Task<IActionResult> UnlinkUser(Guid id, CancellationToken ct)
    {
        var result = await _doctors.UnlinkUserAsync(id, ct);
        return ToResponse(result);
    }

    // --- Lịch làm việc (mẫu tuần) của bác sĩ (WS-02) ---

    /// <summary>Danh sách khung giờ làm việc của một bác sĩ.</summary>
    [HttpGet("{id:guid}/schedules")]
    public async Task<IActionResult> GetSchedules(Guid id, CancellationToken ct)
    {
        var result = await _schedules.GetByDoctorAsync(id, ct);
        return ToResponse(result);
    }

    /// <summary>Thêm một khung giờ làm việc cho bác sĩ.</summary>
    [Authorize(Roles = Roles.ManageStaff)]
    [HttpPost("{id:guid}/schedules")]
    public async Task<IActionResult> CreateSchedule(
        Guid id, [FromBody] CreateDoctorScheduleRequest request, CancellationToken ct)
    {
        var result = await _schedules.CreateAsync(id, request, ct);
        return ToResponse(result, StatusCodes.Status201Created);
    }

    /// <summary>Cập nhật một khung giờ làm việc của bác sĩ.</summary>
    [Authorize(Roles = Roles.ManageStaff)]
    [HttpPut("{id:guid}/schedules/{scheduleId:guid}")]
    public async Task<IActionResult> UpdateSchedule(
        Guid id, Guid scheduleId, [FromBody] UpdateDoctorScheduleRequest request, CancellationToken ct)
    {
        var result = await _schedules.UpdateAsync(id, scheduleId, request, ct);
        return ToResponse(result);
    }

    /// <summary>Xoá một khung giờ làm việc của bác sĩ.</summary>
    [Authorize(Roles = Roles.ManageStaff)]
    [HttpDelete("{id:guid}/schedules/{scheduleId:guid}")]
    public async Task<IActionResult> DeleteSchedule(Guid id, Guid scheduleId, CancellationToken ct)
    {
        var result = await _schedules.DeleteAsync(id, scheduleId, ct);
        return ToResponse(result);
    }
}
