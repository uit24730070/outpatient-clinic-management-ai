using System.Text.Json.Serialization;
using ClinicManagement.Domain.Billing;

namespace ClinicManagement.Application.Billing.Dtos;

/// <summary>Yêu cầu thu tiền hoá đơn.</summary>
/// <remarks><see cref="PaymentMethod"/> nhận chuỗi ("Cash"/"Card"/"Transfer") để khớp FE (khác quy ước enum-số).</remarks>
public sealed record PayInvoiceRequest(
    [property: JsonConverter(typeof(JsonStringEnumConverter))] PaymentMethod PaymentMethod);
