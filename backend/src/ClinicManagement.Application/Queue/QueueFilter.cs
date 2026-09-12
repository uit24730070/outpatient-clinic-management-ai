using ClinicManagement.Domain.Queue;

namespace ClinicManagement.Application.Queue;

/// <summary>Bộ lọc danh sách vé hàng đợi. <see cref="Date"/> null ⇒ mặc định hôm nay (giờ phòng khám).</summary>
public sealed record QueueFilter(
    DateOnly? Date,
    Guid? RoomId,
    Guid? DoctorId,
    QueueTicketStatus? Status);
