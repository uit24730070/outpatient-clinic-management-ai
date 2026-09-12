namespace ClinicManagement.Application.Billing.Dtos;

/// <summary>Yêu cầu hoàn tiền hoá đơn đã thu (ADR 0022, REF-01).</summary>
public sealed record RefundInvoiceRequest(string Reason);
