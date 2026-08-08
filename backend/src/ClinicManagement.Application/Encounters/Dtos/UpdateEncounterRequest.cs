namespace ClinicManagement.Application.Encounters.Dtos;

/// <summary>Dữ liệu sửa phiếu khám (nội dung + thay toàn bộ cụm đơn thuốc). Chỉ khi phiếu còn Draft.</summary>
public sealed record UpdateEncounterRequest(
    string? Symptoms,
    string Diagnosis,
    string? Notes,
    IReadOnlyList<PrescriptionItemRequest>? PrescriptionItems);
