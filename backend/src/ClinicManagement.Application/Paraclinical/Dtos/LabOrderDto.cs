using ClinicManagement.Domain.Paraclinical;

namespace ClinicManagement.Application.Paraclinical.Dtos;

/// <summary>Dữ liệu phiếu chỉ định cận lâm sàng trả về cho client (kèm cụm mục).</summary>
public sealed record LabOrderDto(
    Guid Id,
    string Code,
    Guid? EncounterId,
    Guid? AppointmentId,
    Guid? VisitId,
    Guid PatientId,
    string? PatientName,
    Guid? DoctorId,
    string? DoctorName,
    LabOrderStatus Status,
    string? Note,
    decimal TotalAmount,
    DateTimeOffset? InvoicedAt,
    DateTimeOffset? PaidAt,
    IReadOnlyList<LabOrderItemDto> Items,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
