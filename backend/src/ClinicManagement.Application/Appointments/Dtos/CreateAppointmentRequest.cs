namespace ClinicManagement.Application.Appointments.Dtos;

/// <summary>Dữ liệu đầu vào để đặt lịch khám mới.</summary>
public sealed record CreateAppointmentRequest(
    Guid PatientId,
    Guid DoctorId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string? Reason,
    Guid? ServicePriceId = null);
