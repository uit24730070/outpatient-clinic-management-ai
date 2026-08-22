namespace ClinicManagement.Application.Medications.Dtos;

/// <summary>Dữ liệu đầu vào để cập nhật thuốc.</summary>
public sealed record UpdateMedicationRequest(
    string Name,
    string ActiveIngredient,
    string Unit,
    int ReorderLevel,
    string? Description,
    decimal SalePrice = 0m);
