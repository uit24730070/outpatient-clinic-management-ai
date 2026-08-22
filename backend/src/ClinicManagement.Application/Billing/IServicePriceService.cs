using ClinicManagement.Application.Billing.Dtos;
using ClinicManagement.Domain.Billing;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Application.Billing;

public interface IServicePriceService
{
    Task<Result<ServicePriceDto>> CreateAsync(CreateServicePriceRequest request, CancellationToken ct = default);
    Task<Result<PagedResult<ServicePriceDto>>> GetListAsync(
        int page, int pageSize, string? search, ServiceCategory? category, CancellationToken ct = default);
    Task<Result<ServicePriceDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<ServicePriceDto>> UpdateAsync(Guid id, UpdateServicePriceRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
