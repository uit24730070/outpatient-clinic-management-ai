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
        string? reason,
        Guid? servicePriceId = null,
        string? serviceName = null,
        decimal? servicePrice = null,
        Guid? visitId = null,
        Guid? roomId = null)
    {
        PatientId = patientId;
        DoctorId = doctorId;
        StartTime = startTime;
        EndTime = endTime;
        Reason = reason;
        ServicePriceId = servicePriceId;
        ServiceName = serviceName;
        ServicePrice = servicePrice;
        VisitId = visitId;
        RoomId = roomId;
        Status = AppointmentStatus.Scheduled;
    }

    public Guid PatientId { get; private set; }
    public Guid DoctorId { get; private set; }

    /// <summary>
    /// Lượt tiếp nhận gom lịch này (nếu thuộc một lượt nhiều dịch vụ — ADR 0017); null với lịch lẻ
    /// (tương thích lịch tạo trước Sprint 17 — mỗi lịch lẻ coi như "lượt một dịch vụ").
    /// </summary>
    public Guid? VisitId { get; private set; }

    /// <summary>Gắn/gỡ lượt tiếp nhận cho lịch này.</summary>
    public void SetVisit(Guid? visitId) => VisitId = visitId;
    public DateTimeOffset StartTime { get; private set; }
    public DateTimeOffset EndTime { get; private set; }

    /// <summary>Lý do khám (tuỳ chọn).</summary>
    public string? Reason { get; private set; }

    /// <summary>Dịch vụ khám lễ tân đăng ký khi đặt lịch (bảng giá loại Consultation); null nếu chưa gắn (ADR 0016).</summary>
    public Guid? ServicePriceId { get; private set; }

    /// <summary>Tên dịch vụ khám — snapshot lúc gắn (đổi giá/tên sau không ảnh hưởng lịch cũ).</summary>
    public string? ServiceName { get; private set; }

    /// <summary>Đơn giá dịch vụ khám (VND) — snapshot lúc gắn.</summary>
    public decimal? ServicePrice { get; private set; }

    /// <summary>Gắn/đổi dịch vụ khám (snapshot tên/giá). Truyền null để gỡ.</summary>
    public void SetService(Guid? servicePriceId, string? serviceName, decimal? servicePrice)
    {
        ServicePriceId = servicePriceId;
        ServiceName = serviceName;
        ServicePrice = servicePrice;
    }

    /// <summary>Phòng khám gán cho lịch này (tuỳ chọn — ADR 0018); null nếu chưa gán.</summary>
    public Guid? RoomId { get; private set; }

    /// <summary>Gán/đổi phòng khám cho lịch. Truyền null để gỡ.</summary>
    public void SetRoom(Guid? roomId) => RoomId = roomId;

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

    /// <summary>
    /// Thời điểm đã lập hoá đơn cho dịch vụ khám này — null nếu chưa lập. Cờ chống lập hoá đơn trùng
    /// (như <c>LabOrder.InvoicedAt</c>), cần khi một hoá đơn ở cấp Lượt tiếp nhận có
    /// thể gộp nhiều dịch vụ khám cùng lúc (ADR 0017/0021).
    /// </summary>
    public DateTimeOffset? InvoicedAt { get; private set; }

    /// <summary>
    /// Đánh dấu đã lập hoá đơn cho dịch vụ khám này. Chỉ đặt một lần — đã đặt → lỗi để service map 409
    /// (chống lập hoá đơn trùng cho cùng một dịch vụ khám).
    /// </summary>
    public Result MarkInvoiced(DateTimeOffset when)
    {
        if (InvoicedAt is not null)
            return Result.Failure(Error.Conflict(
                "Billing.AppointmentAlreadyInvoiced", "Dịch vụ khám này đã lập hoá đơn."));

        InvoicedAt = when;
        return Result.Success();
    }

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
