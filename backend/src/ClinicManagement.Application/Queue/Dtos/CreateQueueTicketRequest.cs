namespace ClinicManagement.Application.Queue.Dtos;

/// <summary>
/// Dữ liệu đầu vào để lấy số hàng đợi. <see cref="AppointmentId"/> null với khách vãng lai (không lịch).
/// Phòng/bác sĩ tuỳ chọn — có thể gán ngay hoặc điều phối sau.
/// </summary>
public sealed record CreateQueueTicketRequest(
    Guid PatientId,
    Guid? AppointmentId,
    Guid? RoomId,
    Guid? DoctorId);
