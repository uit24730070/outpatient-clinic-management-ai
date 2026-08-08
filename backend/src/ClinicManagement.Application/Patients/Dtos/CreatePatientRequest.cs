using ClinicManagement.Domain.Patients;

namespace ClinicManagement.Application.Patients.Dtos;

/// <summary>Dữ liệu đầu vào để tạo bệnh nhân mới.</summary>
public sealed record CreatePatientRequest(
    string FullName,
    DateOnly? DateOfBirth,
    Gender Gender,
    string? PhoneNumber,
    string? Address);
