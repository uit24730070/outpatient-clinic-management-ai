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
