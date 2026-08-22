using ClinicManagement.Application.Billing;
using ClinicManagement.Application.Billing.Dtos;
using ClinicManagement.Domain.Billing;
using ClinicManagement.WebApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.WebApi.Controllers;

[Authorize(Roles = Roles.ManageBilling)]
[Route("api/invoices")]
public sealed class InvoicesController : ApiControllerBase
{
    private readonly IInvoiceService _invoices;

    public InvoicesController(IInvoiceService invoices) => _invoices = invoices;

    /// <summary>Lập hoá đơn từ một phiếu khám đã hoàn tất (tự điền công khám + thuốc đã cấp).</summary>
    [HttpPost("from-encounter/{encounterId:guid}")]
    public async Task<IActionResult> CreateFromEncounter(Guid encounterId, CancellationToken ct)
    {
        var result = await _invoices.CreateFromEncounterAsync(encounterId, ct);
        return ToResponse(result, StatusCodes.Status201Created);
    }

    /// <summary>Tạo hoá đơn dịch vụ lẻ (không gắn phiếu khám).</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceRequest request, CancellationToken ct)
    {
        var result = await _invoices.CreateAsync(request, ct);
        return ToResponse(result, StatusCodes.Status201Created);
    }

    /// <summary>Danh sách hoá đơn có phân trang + lọc theo bệnh nhân/lượt/trạng thái/khoảng ngày lập.</summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? patientId = null,
        [FromQuery] Guid? appointmentId = null,
        [FromQuery] InvoiceStatus? status = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken ct = default)
    {
        var result = await _invoices.GetListAsync(page, pageSize, patientId, appointmentId, status, from, to, ct);
        return ToResponse(result);
    }

    /// <summary>Chi tiết một hoá đơn.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _invoices.GetByIdAsync(id, ct);
        return ToResponse(result);
    }

    /// <summary>Gom các hoá đơn của một lượt tiếp đón + tổng đã lập/đã thu/còn nợ.</summary>
    [HttpGet("by-appointment/{appointmentId:guid}")]
    public async Task<IActionResult> GetByAppointment(Guid appointmentId, CancellationToken ct)
    {
        var result = await _invoices.GetByAppointmentAsync(appointmentId, ct);
        return ToResponse(result);
    }

    /// <summary>Sửa cụm dòng dịch vụ + ghi chú của hoá đơn (chỉ khi còn Draft).</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInvoiceRequest request, CancellationToken ct)
    {
        var result = await _invoices.UpdateAsync(id, request, ct);
        return ToResponse(result);
    }

    /// <summary>Thu tiền hoá đơn (Draft → Paid, chọn phương thức).</summary>
    [HttpPost("{id:guid}/pay")]
    public async Task<IActionResult> Pay(Guid id, [FromBody] PayInvoiceRequest request, CancellationToken ct)
    {
        var result = await _invoices.PayAsync(id, request, ct);
        return ToResponse(result);
    }

    /// <summary>Huỷ hoá đơn (Draft → Cancelled).</summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var result = await _invoices.CancelAsync(id, ct);
        return ToResponse(result);
    }

    /// <summary>Xoá mềm hoá đơn (chỉ khi còn Draft).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _invoices.DeleteAsync(id, ct);
        return ToResponse(result);
    }
}
