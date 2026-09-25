using ClinicManagement.Application.Appointments.Dtos;
using ClinicManagement.Domain.Appointments;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Application.Appointments;

/// <summary>Bộ lọc danh sách lịch khám (đều tuỳ chọn).</summary>
public sealed record AppointmentFilter(
    int Page = 1,
    int PageSize = 20,
    DateOnly? Date = null,
    Guid? DoctorId = null,
    Guid? PatientId = null,
    AppointmentStatus? Status = null,
    string? SortBy = null,
    bool SortDesc = false);

public interface IAppointmentService
{
    Task<Result<AppointmentDto>> CreateAsync(CreateAppointmentRequest request, CancellationToken ct = default);
    Task<Result<PagedResult<AppointmentDto>>> GetListAsync(AppointmentFilter filter, CancellationToken ct = default);
    Task<Result<AppointmentDto>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Lượt khám gần nhất của bệnh nhân (để prefill dịch vụ khi tái khám); null nếu chưa có.</summary>
    Task<Result<AppointmentDto?>> GetLastForPatientAsync(Guid patientId, CancellationToken ct = default);
    Task<Result<AppointmentDto>> UpdateAsync(Guid id, UpdateAppointmentRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);

    // Chuyển trạng thái theo máy trạng thái (ADR 0005).
    Task<Result<AppointmentDto>> CheckInAsync(Guid id, CancellationToken ct = default);
    Task<Result<AppointmentDto>> StartAsync(Guid id, CancellationToken ct = default);
    Task<Result<AppointmentDto>> CompleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<AppointmentDto>> CancelAsync(Guid id, CancellationToken ct = default);
    Task<Result<AppointmentDto>> MarkNoShowAsync(Guid id, CancellationToken ct = default);
}
