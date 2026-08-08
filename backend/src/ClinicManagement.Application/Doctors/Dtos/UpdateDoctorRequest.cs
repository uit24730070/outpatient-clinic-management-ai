namespace ClinicManagement.Application.Doctors.Dtos;

/// <summary>Dữ liệu đầu vào để cập nhật bác sĩ. Mã bác sĩ không đổi.</summary>
public sealed record UpdateDoctorRequest(
    string FullName,
    Guid SpecialtyId,
    string? PhoneNumber,
    string? Email);
