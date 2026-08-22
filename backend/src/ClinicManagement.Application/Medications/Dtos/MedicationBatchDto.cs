namespace ClinicManagement.Application.Medications.Dtos;

/// <summary>Dữ liệu một lô thuốc: số lô, hạn dùng, tồn hiện tại.</summary>
public sealed record MedicationBatchDto(
    Guid Id,
    Guid MedicationId,
    string BatchNumber,
    DateOnly ExpiryDate,
    int QuantityOnHand,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
