using ClinicManagement.Domain.Encounters;

namespace ClinicManagement.Application.Encounters.Dtos;

/// <summary>Một dòng đơn thuốc trả về cho client. <see cref="MedicationId"/> có giá trị khi dòng
/// gắn với thuốc trong danh mục (được cấp phát trừ tồn FEFO khi chốt phiếu — ADR 0011).</summary>
public sealed record PrescriptionItemDto(
    Guid? MedicationId,
    string DrugName,
    string Dosage,
    int Quantity,
    string? Instruction)
{
    public static PrescriptionItemDto FromEntity(PrescriptionItem item) =>
        new(item.MedicationId, item.DrugName, item.Dosage, item.Quantity, item.Instruction);
}
