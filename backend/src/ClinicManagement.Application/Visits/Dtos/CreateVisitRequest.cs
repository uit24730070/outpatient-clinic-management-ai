namespace ClinicManagement.Application.Visits.Dtos;

/// <summary>
/// Yêu cầu tạo một lượt tiếp đón kèm 1..n dịch vụ khám (mỗi dịch vụ là một lịch khám con).
/// Hỗ trợ walk-in: lễ tân tạo lượt ngay khi bệnh nhân đến, không cần đặt lịch trước (ADR 0017).
/// </summary>
public sealed record CreateVisitRequest(
    Guid PatientId,
    string? Note,
    IReadOnlyList<VisitServiceLine> Services);

/// <summary>Một dịch vụ khám trong lượt: bác sĩ + khung giờ + dịch vụ khám (loại Consultation).</summary>
public sealed record VisitServiceLine(
    Guid DoctorId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string? Reason,
    Guid? ServicePriceId);

/// <summary>Yêu cầu thêm một dịch vụ khám vào lượt đang mở.</summary>
public sealed record AddVisitServiceRequest(
    Guid DoctorId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string? Reason,
    Guid? ServicePriceId);
