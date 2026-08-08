using ClinicManagement.Domain.Specialties;

namespace ClinicManagement.Application.Specialties.Dtos;

/// <summary>Dữ liệu chuyên khoa trả về cho client.</summary>
public sealed record SpecialtyDto(
    Guid Id,
    string Name,
    string? Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public static SpecialtyDto FromEntity(Specialty s) => new(
        s.Id, s.Name, s.Description, s.CreatedAt, s.UpdatedAt);
}
