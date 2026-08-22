namespace ClinicManagement.Application.Medications.Dtos;

/// <summary>Dữ liệu đầu vào để tạo thuốc mới.</summary>
public sealed record CreateMedicationRequest(
    string Name,
    string ActiveIngredient,
    string Unit,
    int ReorderLevel,
    string? Description);
