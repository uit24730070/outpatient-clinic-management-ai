using ClinicManagement.Application.Appointments.Dtos;
using ClinicManagement.Domain.Paraclinical;
using ClinicManagement.Domain.Visits;

namespace ClinicManagement.Application.Visits.Dtos;

/// <summary>
/// Chi tiết một lượt tiếp đón: các dịch vụ khám (appointment con) + tổng viện phí gom cả lượt
/// (tính phía server từ các hoá đơn gắn với lịch trong lượt). Hoá đơn đã huỷ không tính vào
/// <see cref="TotalBilled"/> (ADR 0017).
/// </summary>
public sealed record VisitDto(
    Guid Id,
    string Code,
    Guid PatientId,
    string? PatientName,
    VisitStatus Status,
    string? Note,
    IReadOnlyList<AppointmentDto> Appointments,
    IReadOnlyList<VisitLabOrderDto> LabOrders,
    decimal TotalBilled,
    decimal TotalPaid,
    decimal TotalOutstanding,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

/// <summary>Phiếu chỉ định CLS gắn lượt (tóm tắt hiển thị trong chi tiết lượt, ADR 0017).</summary>
public sealed record VisitLabOrderDto(
    Guid Id,
    string Code,
    LabOrderStatus Status,
    decimal TotalAmount,
    DateTimeOffset? InvoicedAt,
    int ItemCount);

/// <summary>Dòng danh sách lượt tiếp đón (nhẹ, không nạp chi tiết dịch vụ/viện phí).</summary>
public sealed record VisitListItemDto(
    Guid Id,
    string Code,
    Guid PatientId,
    string? PatientName,
    VisitStatus Status,
    string? Note,
    int ServiceCount,
    DateTimeOffset CreatedAt);
