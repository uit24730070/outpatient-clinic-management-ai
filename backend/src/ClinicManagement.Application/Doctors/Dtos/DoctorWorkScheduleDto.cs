namespace ClinicManagement.Application.Doctors.Dtos;

/// <summary>Dữ liệu một khung giờ làm việc của bác sĩ, kèm tên phòng (nếu gắn).</summary>
public sealed record DoctorWorkScheduleDto(
    Guid Id,
    Guid DoctorId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Guid? RoomId,
    string? RoomName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
