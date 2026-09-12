namespace ClinicManagement.Application.Vitals.Dtos;

/// <summary>Dữ liệu sinh hiệu trả về cho client (kèm BMI tính sẵn + tên người đo).</summary>
public sealed record VitalsDto(
    Guid Id,
    Guid AppointmentId,
    Guid PatientId,
    decimal? HeightCm,
    decimal? WeightKg,
    decimal? Bmi,
    decimal? TemperatureC,
    int? Pulse,
    int? BloodPressureSystolic,
    int? BloodPressureDiastolic,
    int? SpO2,
    int? RespiratoryRate,
    string? Notes,
    DateTimeOffset MeasuredAt,
    Guid MeasuredBy,
    string? MeasuredByName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
