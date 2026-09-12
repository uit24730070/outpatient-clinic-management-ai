using ClinicManagement.Application.Queue.Dtos;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Application.Queue;

public interface IQueueService
{
    /// <summary>Lấy số hàng đợi mới (cấp số tuần tự theo ngày). Hỗ trợ khách vãng lai (không lịch).</summary>
    Task<Result<QueueTicketDto>> CreateAsync(CreateQueueTicketRequest request, CancellationToken ct = default);

    /// <summary>Danh sách vé theo ngày/phòng/bác sĩ/trạng thái (mặc định hôm nay), sắp theo số thứ tự.</summary>
    Task<Result<IReadOnlyList<QueueTicketDto>>> GetListAsync(QueueFilter filter, CancellationToken ct = default);

    /// <summary>Gán/đổi phòng &amp; bác sĩ cho vé (điều phối).</summary>
    Task<Result<QueueTicketDto>> AssignAsync(Guid id, AssignQueueTicketRequest request, CancellationToken ct = default);

    Task<Result<QueueTicketDto>> CallAsync(Guid id, CancellationToken ct = default);
    Task<Result<QueueTicketDto>> StartAsync(Guid id, CancellationToken ct = default);
    Task<Result<QueueTicketDto>> DoneAsync(Guid id, CancellationToken ct = default);
    Task<Result<QueueTicketDto>> SkipAsync(Guid id, CancellationToken ct = default);
}
