using ClinicManagement.Domain.Queue;

namespace ClinicManagement.Application.Queue.Dtos;

/// <summary>Dữ liệu vé hàng đợi trả về cho client (kèm tên bệnh nhân/phòng/bác sĩ).</summary>
public sealed record QueueTicketDto(
    Guid Id,
    DateOnly TicketDate,
    int Number,
    Guid PatientId,
    string? PatientName,
    Guid? AppointmentId,
    Guid? RoomId,
    string? RoomName,
    Guid? DoctorId,
    string? DoctorName,
    QueueTicketStatus Status,
    DateTimeOffset? CalledAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
