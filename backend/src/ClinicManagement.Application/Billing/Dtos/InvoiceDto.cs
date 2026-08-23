using ClinicManagement.Domain.Billing;

namespace ClinicManagement.Application.Billing.Dtos;

/// <summary>Dữ liệu hoá đơn trả về cho client (kèm cụm dòng).</summary>
public sealed record InvoiceDto(
    Guid Id,
    string Code,
    Guid PatientId,
    string? PatientName,
    Guid? EncounterId,
    Guid? AppointmentId,
    Guid? VisitId,
    InvoiceStatus Status,
    decimal TotalAmount,
    DateTimeOffset? PaidAt,
    PaymentMethod? PaymentMethod,
    string? Note,
    IReadOnlyList<InvoiceItemDto> Items,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
