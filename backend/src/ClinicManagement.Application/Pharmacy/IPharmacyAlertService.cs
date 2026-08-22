using ClinicManagement.Application.Pharmacy.Dtos;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Application.Pharmacy;

/// <summary>Tổng hợp cảnh báo kho dược: tồn thấp và lô sắp/đã hết hạn (ADR 0011, P2).</summary>
public interface IPharmacyAlertService
{
    /// <param name="expiringInDays">Số ngày tới để coi là "sắp hết hạn" (lô đã hết hạn luôn được liệt kê).</param>
    Task<Result<PharmacyAlertsDto>> GetAlertsAsync(int expiringInDays, CancellationToken ct = default);
}
