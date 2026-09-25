using ClinicManagement.Application.Paraclinical.Dtos;
using ClinicManagement.Domain.Paraclinical;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Application.Paraclinical;

public interface ILabOrderService
{
    /// <summary>Tạo phiếu chỉ định CLS từ một phiếu khám (đang Draft): snapshot tên/giá dịch vụ Paraclinical.</summary>
    Task<Result<LabOrderDto>> CreateFromEncounterAsync(CreateLabOrderRequest request, CancellationToken ct = default);

    /// <summary>Đăng ký CLS walk-in (lễ tân): không cần phiếu khám/bác sĩ, có thể gắn lượt tiếp nhận (ADR 0016).</summary>
    Task<Result<LabOrderDto>> CreateWalkInAsync(CreateWalkInLabOrderRequest request, CancellationToken ct = default);

    /// <summary>Danh sách phiếu chỉ định có phân trang + lọc theo phiếu khám/bệnh nhân/lượt tiếp nhận/trạng thái.</summary>
    Task<Result<PagedResult<LabOrderDto>>> GetListAsync(
        int page, int pageSize, Guid? encounterId, Guid? patientId, Guid? visitId, LabOrderStatus? status,
        CancellationToken ct = default);

    /// <summary>Chi tiết một phiếu chỉ định.</summary>
    Task<Result<LabOrderDto>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Nhập kết quả cho một mục chỉ định; chuyển vòng đời mục/phiếu.</summary>
    Task<Result<LabOrderDto>> SetItemResultAsync(
        Guid id, Guid itemId, SetLabResultRequest request, CancellationToken ct = default);

    /// <summary>Huỷ phiếu chỉ định (chỉ khi chưa Completed/Cancelled).</summary>
    Task<Result<LabOrderDto>> CancelAsync(Guid id, CancellationToken ct = default);
}
