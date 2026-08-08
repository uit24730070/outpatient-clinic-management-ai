namespace ClinicManagement.Application.Specialties.Dtos;

/// <summary>Dữ liệu đầu vào để tạo chuyên khoa mới.</summary>
public sealed record CreateSpecialtyRequest(
    string Name,
    string? Description);
