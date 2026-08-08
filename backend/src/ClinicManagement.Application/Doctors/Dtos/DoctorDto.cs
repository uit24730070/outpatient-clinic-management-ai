namespace ClinicManagement.Application.Doctors.Dtos;

/// <summary>Dữ liệu bác sĩ trả về cho client, kèm tên chuyên khoa (join).</summary>
public sealed record DoctorDto(
    Guid Id,
    string Code,
    string FullName,
    Guid SpecialtyId,
    string? SpecialtyName,
    string? PhoneNumber,
    string? Email,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
