namespace ClinicManagement.Application.Vitals.Dtos;

/// <summary>
/// Dữ liệu đầu vào để ghi MỘT LẦN ĐO sinh hiệu mới (luôn tạo bản ghi mới — không ghi đè lần đo trước,
/// xem lịch sử). Mọi chỉ số tuỳ chọn — điều dưỡng nhập những gì đo được. BMI tính từ chiều cao/cân nặng
/// ở Domain (không nhận từ client).
/// </summary>
public sealed record RecordVitalsRequest(
    decimal? HeightCm,
    decimal? WeightKg,
    decimal? TemperatureC,
    int? Pulse,
    int? BloodPressureSystolic,
    int? BloodPressureDiastolic,
    int? SpO2,
    int? RespiratoryRate,
    string? Notes);
