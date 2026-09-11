namespace ClinicManagement.Application.Appointments.Dtos;

/// <summary>Dữ liệu đầu vào để đổi lịch (khung giờ/lý do). Không đổi bệnh nhân/bác sĩ.</summary>
public sealed record UpdateAppointmentRequest(
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string? Reason,
    Guid? ServicePriceId = null,
    Guid? RoomId = null);
