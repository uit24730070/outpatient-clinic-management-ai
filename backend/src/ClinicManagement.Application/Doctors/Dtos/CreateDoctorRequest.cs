namespace ClinicManagement.Application.Doctors.Dtos;

/// <summary>Dữ liệu đầu vào để tạo bác sĩ mới.</summary>
public sealed record CreateDoctorRequest(
    string FullName,
    Guid SpecialtyId,
    string? PhoneNumber,
    string? Email);
