using ClinicManagement.Domain.Pharmacy;

namespace ClinicManagement.Application.StockTransactions.Dtos;

/// <summary>Một dòng sổ cái giao dịch tồn, kèm thông tin lô/thuốc để tra cứu.</summary>
public sealed record StockTransactionDto(
    Guid Id,
    Guid MedicationBatchId,
    Guid MedicationId,
    string? MedicationName,
    string? BatchNumber,
    StockTransactionType Type,
    int QuantityDelta,
    string ReferenceType,
    Guid ReferenceId,
    DateTimeOffset OccurredAt);
