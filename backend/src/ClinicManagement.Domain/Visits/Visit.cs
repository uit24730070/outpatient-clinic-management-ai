using ClinicManagement.Domain.Common;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Domain.Visits;

/// <summary>
/// Lượt tiếp đón (lượt khám): gom <b>một lần bệnh nhân đến phòng khám</b>. Một lượt có thể đăng ký
/// <b>nhiều dịch vụ khám</b> (nhiều bác sĩ/chuyên khoa) — mỗi dịch vụ là một
/// <see cref="ClinicManagement.Domain.Appointments.Appointment"/> con tham chiếu
/// <c>VisitId</c> (khoá gom nhóm), kèm cận lâm sàng và hoá đơn cùng lượt (ADR 0017).
/// <para>
/// Là aggregate root gom nhóm nhưng <b>không sở hữu</b> các appointment (chúng có vòng đời/máy trạng
/// thái riêng — ADR 0005) — chỉ giữ mã, snapshot bệnh nhân và trạng thái lượt.
/// Vòng đời: <see cref="VisitStatus.Open"/> → <see cref="VisitStatus.Closed"/>/<see cref="VisitStatus.Cancelled"/>.
/// </para>
/// </summary>
public class Visit : Entity
{
    // EF Core cần constructor không tham số.
    private Visit() { }

    public Visit(string code, Guid patientId, string? note)
    {
        Code = code;
        PatientId = patientId;
        Note = note;
        Status = VisitStatus.Open;
    }

    /// <summary>Mã lượt khám duy nhất, ví dụ LK-000001.</summary>
    public string Code { get; private set; } = null!;

    /// <summary>Bệnh nhân của lượt (snapshot).</summary>
    public Guid PatientId { get; private set; }

    /// <summary>Ghi chú tiếp đón (tuỳ chọn).</summary>
    public string? Note { get; private set; }

    public VisitStatus Status { get; private set; }

    /// <summary>Cập nhật ghi chú tiếp đón.</summary>
    public void UpdateNote(string? note) => Note = note;

    /// <summary>Open → Closed (lượt khám hoàn tất).</summary>
    public Result Close()
    {
        if (Status != VisitStatus.Open)
            return InvalidTransition(nameof(Close));

        Status = VisitStatus.Closed;
        return Result.Success();
    }

    /// <summary>Open → Cancelled (huỷ lượt).</summary>
    public Result Cancel()
    {
        if (Status != VisitStatus.Open)
            return InvalidTransition(nameof(Cancel));

        Status = VisitStatus.Cancelled;
        return Result.Success();
    }

    /// <summary>
    /// Closed → Open (mở lại). Van an toàn cho việc tự động đóng lượt khi bác sĩ chốt phiếu khám cuối
    /// cùng — nếu Lễ tân cần thêm dịch vụ khám/CLS vào đúng lượt đó (vd bác sĩ quên chỉ định), mở lại
    /// thay vì phải tạo lượt mới. Không áp dụng cho <see cref="VisitStatus.Cancelled"/> (huỷ là chốt hẳn).
    /// </summary>
    public Result Reopen()
    {
        if (Status != VisitStatus.Closed)
            return InvalidTransition(nameof(Reopen));

        Status = VisitStatus.Open;
        return Result.Success();
    }

    private Result InvalidTransition(string action) => Result.Failure(Error.Conflict(
        "Visit.InvalidTransition",
        $"Không thể thực hiện '{action}' khi trạng thái lượt là {Status}."));
}
