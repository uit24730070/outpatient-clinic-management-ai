using ClinicManagement.Domain.Encounters;

namespace ClinicManagement.Application.Encounters.Dtos;

/// <summary>Một dòng đơn thuốc trả về cho client.</summary>
public sealed record PrescriptionItemDto(
    string DrugName,
    string Dosage,
    int Quantity,
    string? Instruction)
{
    public static PrescriptionItemDto FromEntity(PrescriptionItem item) =>
        new(item.DrugName, item.Dosage, item.Quantity, item.Instruction);
}
