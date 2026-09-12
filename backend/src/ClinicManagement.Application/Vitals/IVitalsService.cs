using ClinicManagement.Application.Vitals.Dtos;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Application.Vitals;

public interface IVitalsService
{
    /// <summary>Nhập/cập nhật (upsert) sinh hiệu cho một lượt khám. Một bộ sinh hiệu mỗi lượt.</summary>
    Task<Result<VitalsDto>> UpsertAsync(
        Guid appointmentId, UpsertVitalsRequest request, Guid measuredBy, CancellationToken ct = default);

    /// <summary>Lấy sinh hiệu của một lượt khám; <c>data=null</c> nếu chưa đo.</summary>
    Task<Result<VitalsDto?>> GetByAppointmentAsync(Guid appointmentId, CancellationToken ct = default);
}
