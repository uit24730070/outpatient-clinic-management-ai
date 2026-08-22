using ClinicManagement.Domain.Common;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Domain.Encounters;

/// <summary>
/// Phiếu khám (bệnh án của một buổi khám). Gắn <b>1–1</b> với một lịch khám
/// (<see cref="AppointmentId"/>) và lưu <b>snapshot</b> bệnh nhân/bác sĩ suy ra từ lịch đó
/// (xem ADR 0006). Là <b>aggregate root</b> của cụm dòng đơn thuốc (<see cref="PrescriptionItems"/>) —
/// dòng đơn được thay theo cả cụm qua <see cref="ReplaceItems"/>. Vòng đời:
/// <see cref="EncounterStatus.Draft"/> → <see cref="EncounterStatus.Completed"/>.
/// </summary>
public class Encounter : Entity
{
    private readonly List<PrescriptionItem> _prescriptionItems = new();

    // EF Core cần constructor không tham số.
    private Encounter() { }

    public Encounter(
        Guid appointmentId,
        Guid patientId,
        Guid doctorId,
        string? symptoms,
        string diagnosis,
        string? notes)
    {
        AppointmentId = appointmentId;
        PatientId = patientId;
        DoctorId = doctorId;
        Symptoms = symptoms;
        Diagnosis = diagnosis;
        Notes = notes;
        Status = EncounterStatus.Draft;
    }

    /// <summary>Lịch khám gắn với phiếu này (1–1, unique).</summary>
    public Guid AppointmentId { get; private set; }

    /// <summary>Bệnh nhân (snapshot từ lịch khám lúc tạo phiếu).</summary>
    public Guid PatientId { get; private set; }

    /// <summary>Bác sĩ khám (snapshot từ lịch khám lúc tạo phiếu).</summary>
    public Guid DoctorId { get; private set; }

    /// <summary>Triệu chứng (tuỳ chọn).</summary>
    public string? Symptoms { get; private set; }

    /// <summary>Chẩn đoán (bắt buộc).</summary>
    public string Diagnosis { get; private set; } = null!;

    /// <summary>Chỉ định/ghi chú thêm (tuỳ chọn).</summary>
    public string? Notes { get; private set; }

    public EncounterStatus Status { get; private set; }

    /// <summary>
    /// Thời điểm đã cấp phát thuốc theo đơn (trừ tồn FEFO) — null nếu chưa cấp phát.
    /// Đánh dấu để chống cấp phát trùng một đơn (ADR 0011).
    /// </summary>
    public DateTimeOffset? DispensedAt { get; private set; }

    /// <summary>Đã cấp phát thuốc hay chưa.</summary>
    public bool IsDispensed => DispensedAt is not null;

    /// <summary>Cụm dòng đơn thuốc (chỉ đọc từ ngoài; thay cả cụm qua <see cref="ReplaceItems"/>).</summary>
    public IReadOnlyCollection<PrescriptionItem> PrescriptionItems => _prescriptionItems.AsReadOnly();

    /// <summary>Cập nhật nội dung phiếu. Chỉ khi còn <see cref="EncounterStatus.Draft"/>.</summary>
    public Result UpdateDetails(string? symptoms, string diagnosis, string? notes)
    {
        if (Status != EncounterStatus.Draft)
            return InvalidTransition(nameof(UpdateDetails));

        Symptoms = symptoms;
        Diagnosis = diagnosis;
        Notes = notes;
        return Result.Success();
    }

    /// <summary>Thay toàn bộ cụm dòng đơn thuốc. Chỉ khi còn <see cref="EncounterStatus.Draft"/>.</summary>
    public Result ReplaceItems(IEnumerable<PrescriptionItem> items)
    {
        if (Status != EncounterStatus.Draft)
            return InvalidTransition(nameof(ReplaceItems));

        _prescriptionItems.Clear();
        _prescriptionItems.AddRange(items);
        return Result.Success();
    }

    /// <summary>Chốt phiếu: Draft → Completed. Nối máy trạng thái lịch khám ở tầng service (ADR 0006).</summary>
    public Result Complete()
    {
        if (Status != EncounterStatus.Draft)
            return InvalidTransition(nameof(Complete));

        Status = EncounterStatus.Completed;
        return Result.Success();
    }

    /// <summary>Đánh dấu đã cấp phát thuốc (chỉ đặt một lần; các lần sau bỏ qua) — ADR 0011.</summary>
    public void MarkDispensed(DateTimeOffset when) => DispensedAt ??= when;

    private Result InvalidTransition(string action) => Result.Failure(Error.Conflict(
        "Encounter.InvalidTransition",
        $"Không thể thực hiện '{action}' khi trạng thái phiếu khám là {Status}."));
}
