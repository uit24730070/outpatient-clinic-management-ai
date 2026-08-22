using ClinicManagement.Application.Medications.Dtos;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Application.Medications;

public interface IMedicationService
{
    Task<Result<MedicationDto>> CreateAsync(CreateMedicationRequest request, CancellationToken ct = default);
    Task<Result<PagedResult<MedicationDto>>> GetListAsync(
        int page, int pageSize, string? search, CancellationToken ct = default);
    Task<Result<MedicationDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<MedicationDto>> UpdateAsync(Guid id, UpdateMedicationRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Danh sách lô của một thuốc (tồn theo lô + hạn dùng), hạn gần nhất lên đầu.</summary>
    Task<Result<IReadOnlyList<MedicationBatchDto>>> GetBatchesAsync(Guid medicationId, CancellationToken ct = default);
}
