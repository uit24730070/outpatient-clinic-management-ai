using ClinicManagement.Domain.Patients;

namespace ClinicManagement.Application.Patients.Dtos;

/// <summary>Dữ liệu đầu vào để cập nhật bệnh nhân. Mã bệnh nhân không đổi.</summary>
public sealed record UpdatePatientRequest(
    string FullName,
    DateOnly? DateOfBirth,
    Gender Gender,
    string? PhoneNumber,
    string? Address);
