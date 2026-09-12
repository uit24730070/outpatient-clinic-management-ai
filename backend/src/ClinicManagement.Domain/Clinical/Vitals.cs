using ClinicManagement.Domain.Common;

namespace ClinicManagement.Domain.Clinical;

/// <summary>
/// Sinh hiệu (vitals) đo cho một <b>lượt khám</b> (<see cref="AppointmentId"/>, quan hệ 1–1, unique).
/// Điều dưỡng nhập <b>sau check-in</b> — trước khi bác sĩ tạo phiếu khám — nên gắn vào lịch khám,
/// không gắn phiếu khám (Encounter chưa tồn tại lúc đo). Bác sĩ <b>đọc</b> theo <see cref="AppointmentId"/>
/// của phiếu khám (Encounter 1–1 Appointment). Snapshot <see cref="PatientId"/> để tra nhanh (ADR 0019).
/// Một bộ sinh hiệu cho mỗi lượt — cập nhật (upsert) qua <see cref="Update"/>.
/// </summary>
public class Vitals : Entity
{
    // EF Core cần constructor không tham số.
    private Vitals() { }

    public Vitals(Guid appointmentId, Guid patientId, Guid measuredBy)
    {
        AppointmentId = appointmentId;
        PatientId = patientId;
        MeasuredBy = measuredBy;
        MeasuredAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Lượt khám được đo sinh hiệu (1–1, unique).</summary>
    public Guid AppointmentId { get; private set; }

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

    /// <summary>Thời điểm đo (cập nhật mỗi lần ghi).</summary>
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

    /// <summary>Cập nhật (upsert) toàn bộ các chỉ số sinh hiệu + người/thời điểm đo.</summary>
    public void Update(
        decimal? heightCm,
        decimal? weightKg,
        decimal? temperatureC,
        int? pulse,
        int? bloodPressureSystolic,
        int? bloodPressureDiastolic,
        int? spO2,
        int? respiratoryRate,
        string? notes,
        Guid measuredBy)
    {
        HeightCm = heightCm;
        WeightKg = weightKg;
        TemperatureC = temperatureC;
        Pulse = pulse;
        BloodPressureSystolic = bloodPressureSystolic;
        BloodPressureDiastolic = bloodPressureDiastolic;
        SpO2 = spO2;
        RespiratoryRate = respiratoryRate;
        Notes = notes;
        MeasuredBy = measuredBy;
        MeasuredAt = DateTimeOffset.UtcNow;
    }
}
