namespace ClinicManagement.Application.Vitals.Dtos;

/// <summary>
/// Dữ liệu đầu vào để nhập/cập nhật sinh hiệu cho một lượt khám (upsert). Mọi chỉ số tuỳ chọn —
/// điều dưỡng nhập những gì đo được. BMI tính từ chiều cao/cân nặng ở Domain (không nhận từ client).
/// </summary>
public sealed record UpsertVitalsRequest(
    decimal? HeightCm,
    decimal? WeightKg,
    decimal? TemperatureC,
    int? Pulse,
    int? BloodPressureSystolic,
    int? BloodPressureDiastolic,
    int? SpO2,
    int? RespiratoryRate,
    string? Notes);
