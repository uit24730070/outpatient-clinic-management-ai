using ClinicManagement.Domain.Common;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Domain.Appointments;

/// <summary>
/// Lịch khám: gắn một bệnh nhân (<see cref="PatientId"/>) với một bác sĩ (<see cref="DoctorId"/>)
/// trong khung giờ [<see cref="StartTime"/>, <see cref="EndTime"/>). Vòng đời trạng thái tuân theo
/// máy trạng thái ở ADR 0005 — mọi chuyển tiếp đi qua các method dưới đây (nguồn sự thật duy nhất).
/// </summary>
public class Appointment : Entity
{
    // EF Core cần constructor không tham số.
    private Appointment() { }

    public Appointment(
        Guid patientId,
        Guid doctorId,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        string? reason)
    {
        PatientId = patientId;
        DoctorId = doctorId;
        StartTime = startTime;
        EndTime = endTime;
        Reason = reason;
        Status = AppointmentStatus.Scheduled;
    }

    public Guid PatientId { get; private set; }
    public Guid DoctorId { get; private set; }
    public DateTimeOffset StartTime { get; private set; }
    public DateTimeOffset EndTime { get; private set; }

    /// <summary>Lý do khám (tuỳ chọn).</summary>
    public string? Reason { get; private set; }

    public AppointmentStatus Status { get; private set; }

    /// <summary>Thời điểm bệnh nhân check-in (đặt khi chuyển sang <see cref="AppointmentStatus.CheckedIn"/>).</summary>
    public DateTimeOffset? CheckedInAt { get; private set; }

    /// <summary>Các trạng thái còn "chiếm chỗ" khung giờ của bác sĩ (dùng để chống trùng lịch).</summary>
    public static readonly IReadOnlyList<AppointmentStatus> ActiveStatuses = new[]
    {
        AppointmentStatus.Scheduled,
        AppointmentStatus.CheckedIn,
        AppointmentStatus.InProgress,
        AppointmentStatus.Completed
    };

    /// <summary>Đổi khung giờ/lý do. Chỉ cho phép khi lịch chưa bắt đầu khám (Scheduled hoặc CheckedIn).</summary>
    public Result Reschedule(DateTimeOffset startTime, DateTimeOffset endTime, string? reason)
    {
        if (Status is not (AppointmentStatus.Scheduled or AppointmentStatus.CheckedIn))
            return Result.Failure(Error.Conflict(
                "Appointment.InvalidTransition",
                $"Không thể đổi lịch khi trạng thái là {Status}."));

        StartTime = startTime;
        EndTime = endTime;
        Reason = reason;
        return Result.Success();
    }

    /// <summary>Scheduled → CheckedIn (bệnh nhân đã đến).</summary>
    public Result CheckIn()
    {
        if (Status != AppointmentStatus.Scheduled)
            return InvalidTransition(nameof(CheckIn));

        Status = AppointmentStatus.CheckedIn;
        CheckedInAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    /// <summary>CheckedIn → InProgress (bắt đầu khám).</summary>
    public Result Start()
    {
        if (Status != AppointmentStatus.CheckedIn)
            return InvalidTransition(nameof(Start));

        Status = AppointmentStatus.InProgress;
        return Result.Success();
    }

    /// <summary>InProgress → Completed (khám xong).</summary>
    public Result Complete()
    {
        if (Status != AppointmentStatus.InProgress)
            return InvalidTransition(nameof(Complete));

        Status = AppointmentStatus.Completed;
        return Result.Success();
    }

    /// <summary>Scheduled/CheckedIn/InProgress → Cancelled (huỷ lịch).</summary>
    public Result Cancel()
    {
        if (Status is not (AppointmentStatus.Scheduled or AppointmentStatus.CheckedIn or AppointmentStatus.InProgress))
            return InvalidTransition(nameof(Cancel));

        Status = AppointmentStatus.Cancelled;
        return Result.Success();
    }

    /// <summary>Scheduled/CheckedIn → NoShow (bệnh nhân không đến).</summary>
    public Result MarkNoShow()
    {
        if (Status is not (AppointmentStatus.Scheduled or AppointmentStatus.CheckedIn))
            return InvalidTransition(nameof(MarkNoShow));

        Status = AppointmentStatus.NoShow;
        return Result.Success();
    }

    private Result InvalidTransition(string action) => Result.Failure(Error.Conflict(
        "Appointment.InvalidTransition",
        $"Không thể thực hiện '{action}' khi trạng thái là {Status}."));
}
