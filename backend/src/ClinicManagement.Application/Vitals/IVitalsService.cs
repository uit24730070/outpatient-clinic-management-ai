using ClinicManagement.Application.Vitals.Dtos;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Application.Vitals;

public interface IVitalsService
{
    /// <summary>Ghi một lần đo sinh hiệu mới cho lịch khám (luôn tạo bản ghi mới, giữ lịch sử).</summary>
    Task<Result<VitalsDto>> RecordAsync(
        Guid appointmentId, RecordVitalsRequest request, Guid measuredBy, CancellationToken ct = default);

    /// <summary>Lần đo gần nhất của lịch khám (hoặc của cả lượt nếu lịch thuộc một lượt); <c>data=null</c> nếu chưa đo.</summary>
    Task<Result<VitalsDto?>> GetLatestByAppointmentAsync(Guid appointmentId, CancellationToken ct = default);

    /// <summary>Toàn bộ lịch sử đo của lịch khám (hoặc của cả lượt), mới nhất trước.</summary>
    Task<Result<IReadOnlyList<VitalsDto>>> GetHistoryByAppointmentAsync(Guid appointmentId, CancellationToken ct = default);
}
