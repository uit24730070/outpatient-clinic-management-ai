using ClinicManagement.Domain.Billing;
using ClinicManagement.Domain.Paraclinical;

namespace ClinicManagement.Application.Paraclinical.Dtos;

/// <summary>
/// Dữ liệu một mục chỉ định cận lâm sàng trả về cho client. <see cref="Group"/> tra theo
/// <see cref="ServicePriceId"/> hiện tại (KHÔNG snapshot — chỉ để FE chọn giao diện nhập kết quả,
/// đổi nhóm dịch vụ sau vẫn phản ánh đúng, khác <see cref="ServiceName"/>/<see cref="UnitPrice"/> vốn
/// phải đóng băng lúc chỉ định vì ảnh hưởng tiền).
/// </summary>
public sealed record LabOrderItemDto(
    Guid Id,
    Guid ServicePriceId,
    string ServiceName,
    decimal UnitPrice,
    ParaclinicalGroup? Group,
    string? ResultText,
    string? Conclusion,
    IReadOnlyList<LabResultParameterDto> Parameters,
    LabOrderItemStatus Status,
    DateTimeOffset? ResultedAt)
{
    public static LabOrderItemDto FromEntity(LabOrderItem i, ParaclinicalGroup? group) => new(
        i.Id, i.ServicePriceId, i.ServiceName, i.UnitPrice, group,
        i.ResultText, i.Conclusion, i.Parameters.Select(LabResultParameterDto.FromEntity).ToList(),
        i.Status, i.ResultedAt);
}
