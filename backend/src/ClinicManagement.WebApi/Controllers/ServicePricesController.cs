using ClinicManagement.Application.Billing;
using ClinicManagement.Application.Billing.Dtos;
using ClinicManagement.Domain.Billing;
using ClinicManagement.WebApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.WebApi.Controllers;

[Authorize]
[Route("api/service-prices")]
public sealed class ServicePricesController : ApiControllerBase
{
    private readonly IServicePriceService _servicePrices;

    public ServicePricesController(IServicePriceService servicePrices) => _servicePrices = servicePrices;

    /// <summary>Tạo mục bảng giá dịch vụ mới.</summary>
    [Authorize(Roles = Roles.ManageBilling)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServicePriceRequest request, CancellationToken ct)
    {
        var result = await _servicePrices.CreateAsync(request, ct);
        return ToResponse(result, StatusCodes.Status201Created);
    }

    /// <summary>Danh sách dịch vụ có phân trang + tìm kiếm (theo tên/mã) + lọc theo phân loại.</summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] ServiceCategory? category = null,
        CancellationToken ct = default)
    {
        var result = await _servicePrices.GetListAsync(page, pageSize, search, category, ct);
        return ToResponse(result);
    }

    /// <summary>Chi tiết một dịch vụ.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _servicePrices.GetByIdAsync(id, ct);
        return ToResponse(result);
    }

    /// <summary>Cập nhật thông tin dịch vụ.</summary>
    [Authorize(Roles = Roles.ManageBilling)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServicePriceRequest request, CancellationToken ct)
    {
        var result = await _servicePrices.UpdateAsync(id, request, ct);
        return ToResponse(result);
    }

    /// <summary>Xoá mềm một dịch vụ.</summary>
    [Authorize(Roles = Roles.ManageBilling)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _servicePrices.DeleteAsync(id, ct);
        return ToResponse(result);
    }
}
