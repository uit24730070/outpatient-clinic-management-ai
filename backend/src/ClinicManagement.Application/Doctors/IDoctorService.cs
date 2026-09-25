using ClinicManagement.Application.Doctors.Dtos;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Application.Doctors;

public interface IDoctorService
{
    Task<Result<DoctorDto>> CreateAsync(CreateDoctorRequest request, CancellationToken ct = default);
    Task<Result<PagedResult<DoctorDto>>> GetListAsync(
        int page, int pageSize, string? search, string? sortBy = null, bool sortDesc = false, CancellationToken ct = default);
    Task<Result<DoctorDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<DoctorDto>> UpdateAsync(Guid id, UpdateDoctorRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Gắn một tài khoản (role Doctor) vào hồ sơ bác sĩ (quan hệ 1–1).</summary>
    Task<Result<DoctorDto>> LinkUserAsync(Guid doctorId, LinkUserRequest request, CancellationToken ct = default);

    /// <summary>Gỡ liên kết tài khoản khỏi hồ sơ bác sĩ.</summary>
    Task<Result<DoctorDto>> UnlinkUserAsync(Guid doctorId, CancellationToken ct = default);
}
