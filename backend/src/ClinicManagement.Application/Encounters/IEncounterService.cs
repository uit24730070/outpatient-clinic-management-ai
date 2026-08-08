using ClinicManagement.Application.Encounters.Dtos;
using ClinicManagement.Domain.Encounters;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Application.Encounters;

/// <summary>Bộ lọc danh sách phiếu khám (đều tuỳ chọn). Lịch sử khám thường lọc theo <see cref="PatientId"/>.</summary>
public sealed record EncounterFilter(
    int Page = 1,
    int PageSize = 20,
    Guid? PatientId = null,
    Guid? DoctorId = null,
    EncounterStatus? Status = null);

public interface IEncounterService
{
    Task<Result<EncounterDto>> CreateAsync(CreateEncounterRequest request, CancellationToken ct = default);
    Task<Result<PagedResult<EncounterDto>>> GetListAsync(EncounterFilter filter, CancellationToken ct = default);
    Task<Result<EncounterDto>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Lấy phiếu khám theo lịch khám (1–1). NotFound nếu lịch chưa có phiếu.</summary>
    Task<Result<EncounterDto>> GetByAppointmentAsync(Guid appointmentId, CancellationToken ct = default);

    Task<Result<EncounterDto>> UpdateAsync(Guid id, UpdateEncounterRequest request, CancellationToken ct = default);

    /// <summary>Chốt phiếu (Draft → Completed) và khép lịch khám (InProgress → Completed) — ADR 0006.</summary>
    Task<Result<EncounterDto>> CompleteAsync(Guid id, CancellationToken ct = default);
}
