namespace ClinicManagement.Application.Doctors.Dtos;

/// <summary>Dữ liệu đầu vào để thêm một khung giờ làm việc cho bác sĩ.</summary>
public sealed record CreateDoctorScheduleRequest(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Guid? RoomId);

/// <summary>Dữ liệu đầu vào để cập nhật một khung giờ làm việc.</summary>
public sealed record UpdateDoctorScheduleRequest(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Guid? RoomId);
