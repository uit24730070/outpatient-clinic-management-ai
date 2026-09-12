using ClinicManagement.Domain.Appointments;

namespace ClinicManagement.Application.Appointments.Dtos;

/// <summary>Dữ liệu lịch khám trả về cho client, kèm tên bệnh nhân & bác sĩ (join).</summary>
public sealed record AppointmentDto(
    Guid Id,
    Guid PatientId,
    string? PatientName,
    Guid DoctorId,
    string? DoctorName,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string? Reason,
    AppointmentStatus Status,
    DateTimeOffset? CheckedInAt,
    Guid? ServicePriceId,
    string? ServiceName,
    decimal? ServicePrice,
    Guid? RoomId,
    string? RoomName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    // Thời điểm đã lập hoá đơn cho dịch vụ khám này — null nếu chưa lập.
    DateTimeOffset? InvoicedAt,
    // Lượt tiếp đón gom lịch này (ADR 0017) — null với lịch lẻ.
    Guid? VisitId,
    // Số thứ tự hàng đợi cấp cho lịch này (ADR 0019) — null nếu chưa có vé.
    int? QueueNumber);
