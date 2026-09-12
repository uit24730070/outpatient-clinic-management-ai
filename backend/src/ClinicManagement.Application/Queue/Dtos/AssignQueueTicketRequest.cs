namespace ClinicManagement.Application.Queue.Dtos;

/// <summary>Gán/đổi phòng &amp; bác sĩ cho vé hàng đợi (điều phối). Truyền null để bỏ gán.</summary>
public sealed record AssignQueueTicketRequest(
    Guid? RoomId,
    Guid? DoctorId);
