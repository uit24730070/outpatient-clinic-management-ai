namespace ClinicManagement.Application.Paraclinical.Dtos;

/// <summary>Một dòng thông số kết quả có cấu trúc gửi lên khi nhập kết quả xét nghiệm (ADR 0025).</summary>
public sealed record ResultParameterInput(
    string Name,
    string Value,
    string? Unit,
    string? ReferenceRange);
