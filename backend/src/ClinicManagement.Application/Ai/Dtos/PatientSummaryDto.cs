namespace ClinicManagement.Application.Ai.Dtos;

/// <summary>
/// Kết quả tóm tắt lịch sử khám của một bệnh nhân bằng AI.
/// <paramref name="EncounterCount"/> là số phiếu khám gần nhất được dùng làm ngữ cảnh.
/// </summary>
public sealed record PatientSummaryDto(
    Guid PatientId,
    string PatientName,
    string Summary,
    int EncounterCount,
    string Model,
    DateTimeOffset GeneratedAt);
