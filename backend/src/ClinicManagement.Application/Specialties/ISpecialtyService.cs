using ClinicManagement.Application.Specialties.Dtos;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Application.Specialties;

public interface ISpecialtyService
{
    Task<Result<SpecialtyDto>> CreateAsync(CreateSpecialtyRequest request, CancellationToken ct = default);
    Task<Result<PagedResult<SpecialtyDto>>> GetListAsync(int page, int pageSize, string? search, CancellationToken ct = default);
    Task<Result<SpecialtyDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<SpecialtyDto>> UpdateAsync(Guid id, UpdateSpecialtyRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
