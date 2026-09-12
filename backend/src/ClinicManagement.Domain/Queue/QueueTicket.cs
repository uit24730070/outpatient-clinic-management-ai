using ClinicManagement.Domain.Common;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Domain.Queue;

/// <summary>
/// Vé hàng đợi khám trong ngày (ADR 0019). Cấp <see cref="Number"/> tuần tự theo <see cref="TicketDate"/>
/// (phạm vi toàn phòng khám — xem ADR 0019). Gắn tuỳ chọn phòng (<see cref="RoomId"/>)/bác sĩ
/// (<see cref="DoctorId"/>) và lịch khám (<see cref="AppointmentId"/>); khách <b>vãng lai</b> lấy số không
/// cần lịch (<see cref="AppointmentId"/> null). Vòng đời <see cref="QueueTicketStatus"/> qua các method
/// dưới (nguồn sự thật hàng đợi). Không nhân đôi trạng thái lâm sàng của <c>Appointment</c>.
/// </summary>
public class QueueTicket : Entity
{
    // EF Core cần constructor không tham số.
    private QueueTicket() { }

    public QueueTicket(
        DateOnly ticketDate,
        int number,
        Guid patientId,
        Guid? appointmentId,
        Guid? roomId,
        Guid? doctorId)
    {
        TicketDate = ticketDate;
        Number = number;
        PatientId = patientId;
        AppointmentId = appointmentId;
        RoomId = roomId;
        DoctorId = doctorId;
        Status = QueueTicketStatus.Waiting;
    }

    /// <summary>Ngày cấp số (giờ địa phương phòng khám). Số thứ tự tuần tự trong ngày này.</summary>
    public DateOnly TicketDate { get; private set; }

    /// <summary>Số thứ tự trong ngày (bắt đầu từ 1).</summary>
    public int Number { get; private set; }

    /// <summary>Bệnh nhân (snapshot).</summary>
    public Guid PatientId { get; private set; }

    /// <summary>Lịch khám gắn kèm; null với khách vãng lai (lấy số không cần lịch).</summary>
    public Guid? AppointmentId { get; private set; }

    /// <summary>Phòng khám gán cho vé (tuỳ chọn).</summary>
    public Guid? RoomId { get; private set; }

    /// <summary>Bác sĩ gán cho vé (tuỳ chọn).</summary>
    public Guid? DoctorId { get; private set; }

    public QueueTicketStatus Status { get; private set; }

    /// <summary>Thời điểm gọi số (đặt khi chuyển sang <see cref="QueueTicketStatus.Called"/>).</summary>
    public DateTimeOffset? CalledAt { get; private set; }

    /// <summary>Gán/đổi phòng & bác sĩ cho vé (điều phối). Truyền null để giữ chưa gán.</summary>
    public void Assign(Guid? roomId, Guid? doctorId)
    {
        RoomId = roomId;
        DoctorId = doctorId;
    }

    /// <summary>Waiting → Called (gọi số).</summary>
    public Result Call()
    {
        if (Status != QueueTicketStatus.Waiting)
            return InvalidTransition(nameof(Call));

        Status = QueueTicketStatus.Called;
        CalledAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    /// <summary>Called → InProgress (bắt đầu khám).</summary>
    public Result Start()
    {
        if (Status != QueueTicketStatus.Called)
            return InvalidTransition(nameof(Start));

        Status = QueueTicketStatus.InProgress;
        return Result.Success();
    }

    /// <summary>InProgress → Done (rời hàng đợi).</summary>
    public Result Done()
    {
        if (Status != QueueTicketStatus.InProgress)
            return InvalidTransition(nameof(Done));

        Status = QueueTicketStatus.Done;
        return Result.Success();
    }

    /// <summary>Waiting/Called → Skipped (bỏ qua khi không đáp).</summary>
    public Result Skip()
    {
        if (Status is not (QueueTicketStatus.Waiting or QueueTicketStatus.Called))
            return InvalidTransition(nameof(Skip));

        Status = QueueTicketStatus.Skipped;
        return Result.Success();
    }

    private Result InvalidTransition(string action) => Result.Failure(Error.Conflict(
        "Queue.InvalidTransition",
        $"Không thể thực hiện '{action}' khi trạng thái vé là {Status}."));
}
