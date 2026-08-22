using ClinicManagement.Application.StockTransactions.Dtos;
using ClinicManagement.Domain.Pharmacy;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Application.StockTransactions;

public interface IStockTransactionService
{
    /// <summary>Sổ cái giao dịch tồn (mới nhất trước), lọc theo thuốc và/hoặc loại giao dịch.</summary>
    Task<Result<PagedResult<StockTransactionDto>>> GetListAsync(
        int page, int pageSize, Guid? medicationId, StockTransactionType? type, CancellationToken ct = default);
}
