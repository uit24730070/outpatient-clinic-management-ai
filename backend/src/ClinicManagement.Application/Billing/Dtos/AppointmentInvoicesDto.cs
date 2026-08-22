namespace ClinicManagement.Application.Billing.Dtos;

/// <summary>
/// Gom các hoá đơn của một lượt tiếp đón (<see cref="AppointmentId"/>) kèm tổng tiền tính phía server.
/// Hoá đơn đã huỷ (Cancelled) không tính vào <see cref="TotalBilled"/>.
/// </summary>
public sealed record AppointmentInvoicesDto(
    Guid AppointmentId,
    IReadOnlyList<InvoiceDto> Invoices,
    decimal TotalBilled,
    decimal TotalPaid,
    decimal TotalOutstanding);
