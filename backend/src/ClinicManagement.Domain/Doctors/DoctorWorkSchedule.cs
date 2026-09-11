using ClinicManagement.Domain.Common;

namespace ClinicManagement.Domain.Doctors;

/// <summary>
/// Khung giờ làm việc của một bác sĩ theo <b>mẫu lặp hằng tuần</b> (ADR 0018): mỗi bản ghi là một
/// khung [<see cref="StartTime"/>, <see cref="EndTime"/>) trong một <see cref="DayOfWeek"/>, tuỳ chọn
/// gắn phòng (<see cref="RoomId"/>). Ràng buộc đặt lịch (giờ khám phải nằm trong một khung) áp ở
/// <c>AppointmentService</c>. Ngày nghỉ đột xuất (override) là phần nợ.
/// </summary>
public class DoctorWorkSchedule : Entity
{
    // EF Core cần constructor không tham số.
    private DoctorWorkSchedule() { }

    public DoctorWorkSchedule(
        Guid doctorId, DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, Guid? roomId)
    {
        DoctorId = doctorId;
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        RoomId = roomId;
    }

    /// <summary>Bác sĩ sở hữu khung làm việc này.</summary>
    public Guid DoctorId { get; private set; }

    /// <summary>Thứ trong tuần (lưu dạng chuỗi ở CSDL).</summary>
    public DayOfWeek DayOfWeek { get; private set; }

    /// <summary>Giờ bắt đầu (giờ địa phương phòng khám).</summary>
    public TimeOnly StartTime { get; private set; }

    /// <summary>Giờ kết thúc (giờ địa phương phòng khám); phải sau <see cref="StartTime"/>.</summary>
    public TimeOnly EndTime { get; private set; }

    /// <summary>Phòng gắn cho khung này (tuỳ chọn).</summary>
    public Guid? RoomId { get; private set; }

    /// <summary>Cập nhật khung làm việc.</summary>
    public void UpdateDetails(DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, Guid? roomId)
    {
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        RoomId = roomId;
    }
}
