namespace ClinicManagement.Application.Paraclinical.Dtos;

/// <summary>
/// Dữ liệu chỉ định cận lâm sàng từ một phiếu khám. Mỗi dòng tham chiếu một dịch vụ CLS
/// (bảng giá loại <c>Paraclinical</c>); tên và giá được snapshot tại thời điểm chỉ định.
/// </summary>
public sealed record CreateLabOrderRequest(
    Guid EncounterId,
    string? Note,
    IReadOnlyList<CreateLabOrderItemRequest> Items);

/// <summary>Một dòng chỉ định (một dịch vụ CLS).</summary>
public sealed record CreateLabOrderItemRequest(Guid ServicePriceId);

/// <summary>
/// Dữ liệu đăng ký cận lâm sàng <b>walk-in</b> do lễ tân (ADR 0016): không cần phiếu khám/bác sĩ.
/// Chỉ cần bệnh nhân + (tuỳ chọn) lượt tiếp đón + các dịch vụ CLS (bảng giá loại <c>Paraclinical</c>).
/// </summary>
public sealed record CreateWalkInLabOrderRequest(
    Guid PatientId,
    Guid? AppointmentId,
    string? Note,
    IReadOnlyList<CreateLabOrderItemRequest> Items);
