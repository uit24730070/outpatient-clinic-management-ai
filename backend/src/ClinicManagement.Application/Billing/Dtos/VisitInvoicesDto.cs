namespace ClinicManagement.Application.Billing.Dtos;

/// <summary>
/// Gom các hoá đơn của một lượt tiếp đón (<see cref="VisitId"/>) kèm tổng tiền tính phía server.
/// Hoá đơn đã huỷ (Cancelled) không tính vào <see cref="TotalBilled"/> (ADR 0017).
/// </summary>
public sealed record VisitInvoicesDto(
    Guid VisitId,
    IReadOnlyList<InvoiceDto> Invoices,
    decimal TotalBilled,
    decimal TotalPaid,
    decimal TotalOutstanding);
