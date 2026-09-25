using ClinicManagement.Domain.Paraclinical;

namespace ClinicManagement.Application.Paraclinical.Dtos;

/// <summary>Một thông số kết quả có cấu trúc trả về cho client (ADR 0025).</summary>
public sealed record LabResultParameterDto(
    string Name,
    string Value,
    string? Unit,
    string? ReferenceRange,
    bool IsAbnormal)
{
    public static LabResultParameterDto FromEntity(LabResultParameter p) => new(
        p.Name, p.Value, p.Unit, p.ReferenceRange, p.IsAbnormal);
}
