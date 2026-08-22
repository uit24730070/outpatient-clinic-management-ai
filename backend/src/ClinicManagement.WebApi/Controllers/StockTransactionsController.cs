using ClinicManagement.Application.StockTransactions;
using ClinicManagement.Domain.Pharmacy;
using ClinicManagement.WebApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.WebApi.Controllers;

[Authorize(Roles = Roles.ManagePharmacy)]
[Route("api/stock-transactions")]
public sealed class StockTransactionsController : ApiControllerBase
{
    private readonly IStockTransactionService _transactions;

    public StockTransactionsController(IStockTransactionService transactions) => _transactions = transactions;

    /// <summary>Sổ cái giao dịch tồn (mới nhất trước), lọc theo thuốc và/hoặc loại giao dịch.</summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? medicationId = null,
        [FromQuery] StockTransactionType? type = null,
        CancellationToken ct = default)
    {
        var result = await _transactions.GetListAsync(page, pageSize, medicationId, type, ct);
        return ToResponse(result);
    }
}
