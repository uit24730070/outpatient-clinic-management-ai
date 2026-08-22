namespace ClinicManagement.Application.Encounters.Dtos;

/// <summary>Một dòng đơn thuốc trong yêu cầu tạo/sửa phiếu khám. <see cref="MedicationId"/> tuỳ chọn:
/// khi có phải trỏ tới thuốc tồn tại trong danh mục (kiểm ở service); null = thuốc ngoài danh mục.</summary>
public sealed record PrescriptionItemRequest(
    string DrugName,
    string Dosage,
    int Quantity,
    string? Instruction,
    Guid? MedicationId = null);

/// <summary>
/// Dữ liệu tạo phiếu khám mới. Bệnh nhân/bác sĩ suy ra từ lịch khám (<see cref="AppointmentId"/>),
/// client không gửi (xem ADR 0006).
/// </summary>
public sealed record CreateEncounterRequest(
    Guid AppointmentId,
    string? Symptoms,
    string Diagnosis,
    string? Notes,
    IReadOnlyList<PrescriptionItemRequest>? PrescriptionItems);
