using ClinicManagement.Domain.Common;

namespace ClinicManagement.Domain.Clinical;

/// <summary>
/// Một lần đo sinh hiệu (vitals) — bản ghi <b>bất biến</b> (không sửa sau khi tạo), giữ lại lịch sử: mỗi
/// lần điều dưỡng đo là một dòng mới, kể cả khi đo lại nhiều lần cho cùng một lần đến (bệnh nhân yêu cầu
/// đo lại, chỉ số bất thường cần đo kiểm tra…) — không ghi đè lần đo trước.
/// Gắn với lịch khám (<see cref="AppointmentId"/>) tại thời điểm đo; khi lịch đó thuộc một
/// <b>Lượt tiếp nhận</b> (<see cref="VisitId"/>, ADR 0017) thì mọi lần đo trong lượt (đo từ dịch vụ khám
/// nào cũng vậy — nhiều chuyên khoa cùng một lần đến chỉ cần đo chung) được <b>gom theo VisitId</b>, xem
/// được lịch sử/lần gần nhất từ bất kỳ dịch vụ khám nào trong lượt. Lịch lẻ (không thuộc lượt nào —
/// tương thích trước Sprint 17) gom theo <see cref="AppointmentId"/>.
/// Điều dưỡng nhập <b>sau check-in</b> — trước khi bác sĩ tạo phiếu khám — nên gắn vào lịch khám, không
/// gắn phiếu khám (Encounter chưa tồn tại lúc đo). Snapshot <see cref="PatientId"/> để tra nhanh (ADR 0019).
/// </summary>
public class Vitals : Entity
{
    // EF Core cần constructor không tham số.
    private Vitals() { }

    public Vitals(
        Guid appointmentId,
        Guid patientId,
        Guid measuredBy,
        Guid? visitId,
        decimal? heightCm,
        decimal? weightKg,
        decimal? temperatureC,
        int? pulse,
        int? bloodPressureSystolic,
        int? bloodPressureDiastolic,
        int? spO2,
        int? respiratoryRate,
        string? notes)
    {
        AppointmentId = appointmentId;
        PatientId = patientId;
        MeasuredBy = measuredBy;
        VisitId = visitId;
        HeightCm = heightCm;
        WeightKg = weightKg;
        TemperatureC = temperatureC;
        Pulse = pulse;
        BloodPressureSystolic = bloodPressureSystolic;
        BloodPressureDiastolic = bloodPressureDiastolic;
        SpO2 = spO2;
        RespiratoryRate = respiratoryRate;
        Notes = notes;
        MeasuredAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Lịch khám đo lần này (tại thời điểm đo).</summary>
    public Guid AppointmentId { get; private set; }

    /// <summary>
    /// Lượt tiếp nhận (nếu lịch khám thuộc một lượt) — khoá gom lịch sử đo dùng chung cho mọi dịch vụ
    /// khám trong lượt; null với lịch lẻ (gom theo <see cref="AppointmentId"/>).
    /// </summary>
    public Guid? VisitId { get; private set; }

    /// <summary>Bệnh nhân (snapshot từ lịch khám).</summary>
    public Guid PatientId { get; private set; }

    /// <summary>Chiều cao (cm).</summary>
    public decimal? HeightCm { get; private set; }

    /// <summary>Cân nặng (kg).</summary>
    public decimal? WeightKg { get; private set; }

    /// <summary>Nhiệt độ cơ thể (°C).</summary>
    public decimal? TemperatureC { get; private set; }

    /// <summary>Mạch (lần/phút).</summary>
    public int? Pulse { get; private set; }

    /// <summary>Huyết áp tâm thu (mmHg).</summary>
    public int? BloodPressureSystolic { get; private set; }

    /// <summary>Huyết áp tâm trương (mmHg).</summary>
    public int? BloodPressureDiastolic { get; private set; }

    /// <summary>Độ bão hoà oxy máu SpO2 (%).</summary>
    public int? SpO2 { get; private set; }

    /// <summary>Nhịp thở (lần/phút).</summary>
    public int? RespiratoryRate { get; private set; }

    /// <summary>Ghi chú thêm của điều dưỡng (tuỳ chọn).</summary>
    public string? Notes { get; private set; }

    /// <summary>Thời điểm đo — cố định tại lúc tạo (bản ghi bất biến, không có lần "cập nhật").</summary>
    public DateTimeOffset MeasuredAt { get; private set; }

    /// <summary>Người đo (Id tài khoản điều dưỡng/Admin đã nhập).</summary>
    public Guid MeasuredBy { get; private set; }

    /// <summary>
    /// Chỉ số khối cơ thể (BMI = cân nặng(kg) / chiều cao(m)²), tính từ <see cref="HeightCm"/>/<see cref="WeightKg"/>;
    /// null nếu thiếu chiều cao/cân nặng. Làm tròn 1 chữ số thập phân. Cột tính toán (không lưu — <c>Ignore</c>).
    /// </summary>
    public decimal? Bmi
    {
        get
        {
            if (HeightCm is not > 0 || WeightKg is not > 0) return null;
            var meters = HeightCm.Value / 100m;
            return Math.Round(WeightKg.Value / (meters * meters), 1, MidpointRounding.AwayFromZero);
        }
    }
}
