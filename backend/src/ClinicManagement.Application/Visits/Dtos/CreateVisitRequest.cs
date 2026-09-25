namespace ClinicManagement.Application.Visits.Dtos;

/// <summary>
/// Yêu cầu tạo một lượt tiếp nhận kèm 1..n dịch vụ khám (mỗi dịch vụ là một lịch khám con).
/// Hỗ trợ walk-in: lễ tân tạo lượt ngay khi bệnh nhân đến, không cần đặt lịch trước (ADR 0017).
/// </summary>
public sealed record CreateVisitRequest(
    Guid PatientId,
    string? Note,
    IReadOnlyList<VisitServiceLine> Services,
    IReadOnlyList<Guid>? ParaclinicalServiceIds = null);

/// <summary>
/// Một dịch vụ khám trong lượt: bác sĩ + dịch vụ khám (loại Consultation). Không nhận khung giờ từ
/// lễ tân — walk-in không đặt trước giờ khám thực tế (nhất là lúc đông khách), nên server tự lấy thời
/// điểm tiếp nhận làm mốc; thứ tự khám do số thứ tự hàng đợi quyết định, không phải khung giờ.
/// </summary>
public sealed record VisitServiceLine(
    Guid DoctorId,
    string? Reason,
    Guid? ServicePriceId);

/// <summary>Yêu cầu thêm một dịch vụ khám vào lượt đang mở (cùng lý do không nhận khung giờ ở trên).</summary>
public sealed record AddVisitServiceRequest(
    Guid DoctorId,
    string? Reason,
    Guid? ServicePriceId);
