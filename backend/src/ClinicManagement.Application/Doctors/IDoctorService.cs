using ClinicManagement.Application.Doctors.Dtos;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Application.Doctors;

public interface IDoctorService
{
    Task<Result<DoctorDto>> CreateAsync(CreateDoctorRequest request, CancellationToken ct = default);
    Task<Result<PagedResult<DoctorDto>>> GetListAsync(int page, int pageSize, string? search, CancellationToken ct = default);
    Task<Result<DoctorDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<DoctorDto>> UpdateAsync(Guid id, UpdateDoctorRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
