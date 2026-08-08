using ClinicManagement.Domain.Patients;

namespace ClinicManagement.Application.Patients.Dtos;

/// <summary>Dữ liệu bệnh nhân trả về cho client.</summary>
public sealed record PatientDto(
    Guid Id,
    string Code,
    string FullName,
    DateOnly? DateOfBirth,
    Gender Gender,
    string? PhoneNumber,
    string? Address,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public static PatientDto FromEntity(Patient p) => new(
        p.Id, p.Code, p.FullName, p.DateOfBirth, p.Gender,
        p.PhoneNumber, p.Address, p.CreatedAt, p.UpdatedAt);
}
