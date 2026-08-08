using ClinicManagement.Application.Patients.Dtos;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Application.Patients;

public interface IPatientService
{
    Task<Result<PatientDto>> CreateAsync(CreatePatientRequest request, CancellationToken ct = default);
    Task<Result<PagedResult<PatientDto>>> GetListAsync(int page, int pageSize, string? search, CancellationToken ct = default);
    Task<Result<PatientDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<PatientDto>> UpdateAsync(Guid id, UpdatePatientRequest request, CancellationToken ct = default);
}
