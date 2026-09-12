using ClinicManagement.Domain.Encounters;

namespace ClinicManagement.Application.Encounters.Dtos;

/// <summary>Dữ liệu phiếu khám trả về cho client, kèm tên bệnh nhân/bác sĩ (join) và cụm đơn thuốc.</summary>
public sealed record EncounterDto(
    Guid Id,
    Guid AppointmentId,
    Guid PatientId,
    string? PatientName,
    Guid DoctorId,
    string? DoctorName,
    string? Symptoms,
    string Diagnosis,
    string? Notes,
    EncounterStatus Status,
    IReadOnlyList<PrescriptionItemDto> PrescriptionItems,
    DispenseStatus DispenseStatus,
    DateTimeOffset? ReservedAt,
    DateTimeOffset? MedicationPaidAt,
    DateTimeOffset? DispensedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    // Thời điểm đã lập hoá đơn thuốc từ phiếu này — null nếu chưa lập.
    DateTimeOffset? MedicationInvoicedAt);
