using ClinicManagement.Application.Billing.Dtos;
using ClinicManagement.Domain.Billing;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Application.Billing;

public interface IInvoiceService
{
    /// <summary>Lập hoá đơn từ một phiếu khám đã hoàn tất: tự dựng dòng công khám + dòng thuốc đã cấp.</summary>
    Task<Result<InvoiceDto>> CreateFromEncounterAsync(Guid encounterId, CancellationToken ct = default);

    /// <summary>Lập hoá đơn phí cận lâm sàng từ một phiếu chỉ định (loại Paraclinical, snapshot giá).</summary>
    Task<Result<InvoiceDto>> CreateFromLabOrderAsync(Guid labOrderId, CancellationToken ct = default);

    /// <summary>Tạo hoá đơn dịch vụ lẻ (không gắn phiếu khám).</summary>
    Task<Result<InvoiceDto>> CreateAsync(CreateInvoiceRequest request, CancellationToken ct = default);

    /// <summary>Danh sách hoá đơn có phân trang + lọc theo bệnh nhân/lượt/trạng thái/khoảng ngày lập.</summary>
    Task<Result<PagedResult<InvoiceDto>>> GetListAsync(
        int page, int pageSize, Guid? patientId, Guid? appointmentId, InvoiceStatus? status,
        DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default);

    /// <summary>Chi tiết một hoá đơn.</summary>
    Task<Result<InvoiceDto>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Gom các hoá đơn theo một lịch khám + tổng đã lập/đã thu/còn nợ (tính phía server).</summary>
    Task<Result<AppointmentInvoicesDto>> GetByAppointmentAsync(Guid appointmentId, CancellationToken ct = default);

    /// <summary>Gom các hoá đơn của một lượt tiếp đón + tổng đã lập/đã thu/còn nợ (ADR 0017).</summary>
    Task<Result<VisitInvoicesDto>> GetByVisitAsync(Guid visitId, CancellationToken ct = default);

    /// <summary>Thu tiền toàn bộ hoá đơn còn <c>Draft</c> của một lượt (một phương thức), trả tổng sau thu.</summary>
    Task<Result<VisitInvoicesDto>> PayVisitAsync(Guid visitId, PayInvoiceRequest request, CancellationToken ct = default);

    /// <summary>Sửa cụm dòng dịch vụ + ghi chú của hoá đơn — chỉ khi còn <c>Draft</c>.</summary>
    Task<Result<InvoiceDto>> UpdateAsync(Guid id, UpdateInvoiceRequest request, CancellationToken ct = default);

    /// <summary>Thu tiền: Draft → Paid (ghi phương thức + thời điểm). Sai vòng đời → 409.</summary>
    Task<Result<InvoiceDto>> PayAsync(Guid id, PayInvoiceRequest request, CancellationToken ct = default);

    /// <summary>Huỷ hoá đơn: Draft → Cancelled. Sai vòng đời → 409.</summary>
    Task<Result<InvoiceDto>> CancelAsync(Guid id, CancellationToken ct = default);

    /// <summary>Xoá mềm hoá đơn — chỉ khi còn <c>Draft</c>.</summary>
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
