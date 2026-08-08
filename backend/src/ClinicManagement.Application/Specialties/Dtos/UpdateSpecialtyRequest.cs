namespace ClinicManagement.Application.Specialties.Dtos;

/// <summary>Dữ liệu đầu vào để cập nhật chuyên khoa.</summary>
public sealed record UpdateSpecialtyRequest(
    string Name,
    string? Description);
