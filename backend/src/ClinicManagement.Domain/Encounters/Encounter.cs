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
    /// Trạng thái cấp phát thuốc (ADR 0021, PAY-02). Mặc định <see cref="DispenseStatus.None"/>;
    /// chốt phiếu có thuốc → <see cref="DispenseStatus.Reserved"/> → thu tiền → <see cref="DispenseStatus.Paid"/>
    /// → cấp phát thực → <see cref="DispenseStatus.Dispensed"/>.
    /// </summary>
    public DispenseStatus DispenseStatus { get; private set; } = DispenseStatus.None;

    /// <summary>Thời điểm chốt phiếu giữ tồn (đặt trạng thái Reserved) — null nếu chưa/không cần cấp phát.</summary>
    public DateTimeOffset? ReservedAt { get; private set; }

    /// <summary>Thời điểm đã thu tiền hoá đơn thuốc (Reserved → Paid) — null nếu chưa thu.</summary>
    public DateTimeOffset? MedicationPaidAt { get; private set; }

    /// <summary>
    /// Thời điểm đã cấp phát thuốc theo đơn (trừ tồn FEFO) — null nếu chưa cấp phát.
    /// Đánh dấu để chống cấp phát trùng một đơn (ADR 0011).
    /// </summary>
    public DateTimeOffset? DispensedAt { get; private set; }

    /// <summary>Đã cấp phát thuốc hay chưa.</summary>
    public bool IsDispensed => DispensedAt is not null;

    /// <summary>
    /// Thời điểm đã lập hoá đơn thuốc từ phiếu này — null nếu chưa lập.
    /// Cờ chống lập hoá đơn thuốc trùng (thay lá chắn unique <c>EncounterId</c> đã bỏ ở Mô hình A — ADR 0014 P2).
    /// </summary>
    public DateTimeOffset? MedicationInvoicedAt { get; private set; }

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

    /// <summary>
    /// Giữ tồn khi chốt phiếu có thuốc (None → Reserved), đặt <see cref="ReservedAt"/>. Gọi từ service sau khi
    /// đã kiểm tồn khả dụng đủ (ADR 0021, PAY-02). Chỉ hợp lệ khi đang <see cref="DispenseStatus.None"/>.
    /// </summary>
    public Result MarkReserved(DateTimeOffset when)
    {
        if (DispenseStatus != DispenseStatus.None)
            return Result.Failure(Error.Conflict("Pharmacy.InvalidDispenseTransition",
                $"Không thể giữ tồn khi trạng thái cấp phát là {DispenseStatus}."));

        DispenseStatus = DispenseStatus.Reserved;
        ReservedAt = when;
        return Result.Success();
    }

    /// <summary>
    /// Đánh dấu đã thu tiền hoá đơn thuốc (Reserved → Paid). Idempotent theo hướng an toàn: chỉ chuyển khi đang
    /// <see cref="DispenseStatus.Reserved"/>; trạng thái khác (None/Paid/Dispensed) bỏ qua yên lặng để việc thu
    /// hoá đơn không phụ thuộc thứ tự/không lỗi khi phiếu không có thuốc (ADR 0021, PAY-02).
    /// </summary>
    public void MarkMedicationPaid(DateTimeOffset when)
    {
        if (DispenseStatus != DispenseStatus.Reserved)
            return;

        DispenseStatus = DispenseStatus.Paid;
        MedicationPaidAt = when;
    }

    /// <summary>
    /// Cấp phát thực (Paid → Dispensed): đặt <see cref="DispensedAt"/>. Chỉ khi đã thu tiền
    /// (<see cref="DispenseStatus.Paid"/>). Chưa thu (Reserved) → <c>Pharmacy.NotPaid</c>; None/Dispensed → 409.
    /// Service chạy trừ tồn FEFO + ghi sổ cái sau khi method này thành công (ADR 0021, PAY-02).
    /// </summary>
    public Result MarkDispensed(DateTimeOffset when)
    {
        if (DispenseStatus == DispenseStatus.Reserved)
            return Result.Failure(Error.Conflict("Pharmacy.NotPaid",
                "Chưa thanh toán tiền thuốc; không thể cấp phát."));

        if (DispenseStatus != DispenseStatus.Paid)
            return Result.Failure(Error.Conflict("Pharmacy.InvalidDispenseTransition",
                $"Không thể cấp phát khi trạng thái cấp phát là {DispenseStatus}."));

        DispenseStatus = DispenseStatus.Dispensed;
        DispensedAt = when;
        return Result.Success();
    }

    /// <summary>
    /// Hoàn kho (Dispensed → Returned): đặt lại trạng thái sau khi service nhập lại tồn đúng lô.
    /// Chỉ hợp lệ khi đang <see cref="DispenseStatus.Dispensed"/>; gọi hai lần → 409 (ADR 0022, REF-02).
    /// </summary>
    public Result MarkReturned()
    {
        if (DispenseStatus != DispenseStatus.Dispensed)
            return Result.Failure(Error.Conflict("Pharmacy.InvalidDispenseTransition",
                $"Không thể hoàn kho khi trạng thái cấp phát là {DispenseStatus}."));

        DispenseStatus = DispenseStatus.Returned;
        return Result.Success();
    }

    /// <summary>
    /// Đánh dấu đã lập hoá đơn thuốc từ phiếu này. Chỉ đặt một lần — đã đặt → lỗi để service map 409
    /// (chống lập hoá đơn thuốc trùng, ADR 0014 P2).
    /// </summary>
    public Result MarkMedicationInvoiced(DateTimeOffset when)
    {
        if (MedicationInvoicedAt is not null)
            return Result.Failure(Error.Conflict(
                "Billing.MedicationAlreadyInvoiced", "Phiếu khám này đã lập hoá đơn thuốc."));

        MedicationInvoicedAt = when;
        return Result.Success();
    }

    private Result InvalidTransition(string action) => Result.Failure(Error.Conflict(
        "Encounter.InvalidTransition",
        $"Không thể thực hiện '{action}' khi trạng thái phiếu khám là {Status}."));
}
