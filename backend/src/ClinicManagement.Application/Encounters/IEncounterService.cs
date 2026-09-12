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
    EncounterStatus? Status = null,
    DispenseStatus? DispenseStatus = null);

public interface IEncounterService
{
    Task<Result<EncounterDto>> CreateAsync(CreateEncounterRequest request, CancellationToken ct = default);
    Task<Result<PagedResult<EncounterDto>>> GetListAsync(EncounterFilter filter, CancellationToken ct = default);
    Task<Result<EncounterDto>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Lấy phiếu khám theo lịch khám (1–1). NotFound nếu lịch chưa có phiếu.</summary>
    Task<Result<EncounterDto>> GetByAppointmentAsync(Guid appointmentId, CancellationToken ct = default);

    Task<Result<EncounterDto>> UpdateAsync(Guid id, UpdateEncounterRequest request, CancellationToken ct = default);

    /// <summary>
    /// Chốt phiếu (Draft → Completed) và khép lịch khám (InProgress → Completed) — ADR 0006. Nếu có thuốc gắn
    /// danh mục: giữ tồn (Reserved) sau khi kiểm tồn khả dụng đủ — chưa trừ tồn vật lý (ADR 0021, PAY-02).
    /// </summary>
    Task<Result<EncounterDto>> CompleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Cấp phát thực đơn thuốc đã thu tiền (Paid → Dispensed): trừ tồn FEFO + ghi sổ cái. Dược sĩ thực hiện.
    /// Chưa thu → <c>Pharmacy.NotPaid</c>; không có thuốc → <c>Pharmacy.NothingToDispense</c> (ADR 0021, PAY-02).
    /// </summary>
    Task<Result<EncounterDto>> DispenseAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Hoàn kho đơn đã cấp phát (Dispensed → Returned): nhập lại tồn đúng lô đã trừ + ghi sổ cái bù Return.
    /// Gọi hai lần → 409. Chưa cấp phát → 409 (ADR 0022, REF-02).
    /// </summary>
    Task<Result<EncounterDto>> ReturnStockAsync(Guid id, CancellationToken ct = default);
}
