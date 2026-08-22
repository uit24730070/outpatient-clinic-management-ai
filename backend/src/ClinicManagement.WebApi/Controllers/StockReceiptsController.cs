using ClinicManagement.Application.StockReceipts;
using ClinicManagement.Application.StockReceipts.Dtos;
using ClinicManagement.WebApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.WebApi.Controllers;

[Authorize]
[Route("api/stock-receipts")]
public sealed class StockReceiptsController : ApiControllerBase
{
    private readonly IStockReceiptService _receipts;

    public StockReceiptsController(IStockReceiptService receipts) => _receipts = receipts;

    /// <summary>Tạo phiếu nhập kho: tăng tồn theo lô + ghi sổ cái Import.</summary>
    [Authorize(Roles = Roles.ManagePharmacy)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStockReceiptRequest request, CancellationToken ct)
    {
        var result = await _receipts.CreateAsync(request, ct);
        return ToResponse(result, StatusCodes.Status201Created);
    }

    /// <summary>Danh sách phiếu nhập (mới nhất trước).</summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _receipts.GetListAsync(page, pageSize, ct);
        return ToResponse(result);
    }

    /// <summary>Chi tiết một phiếu nhập kèm các dòng nhập.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _receipts.GetByIdAsync(id, ct);
        return ToResponse(result);
    }
}
