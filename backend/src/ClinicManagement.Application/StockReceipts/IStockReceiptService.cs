using ClinicManagement.Application.StockReceipts.Dtos;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Application.StockReceipts;

public interface IStockReceiptService
{
    /// <summary>Tạo phiếu nhập: tăng/tạo lô theo từng dòng + ghi sổ cái Import (một SaveChanges).</summary>
    Task<Result<StockReceiptDto>> CreateAsync(CreateStockReceiptRequest request, CancellationToken ct = default);
    Task<Result<PagedResult<StockReceiptDto>>> GetListAsync(
        int page, int pageSize, string? sortBy = null, bool sortDesc = false, CancellationToken ct = default);
    Task<Result<StockReceiptDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
}
