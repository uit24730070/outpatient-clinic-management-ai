namespace ClinicManagement.Application.Encounters.Dtos;

/// <summary>Một dòng thuốc muốn hoàn kho — số lượng phải &gt; 0 và không vượt số lượng đã cấp phát.</summary>
public sealed record ReturnStockItemRequest(Guid MedicationId, int Quantity);

/// <summary>
/// Yêu cầu hoàn kho đơn thuốc đã cấp phát — bắt buộc lý do (audit tối thiểu, ADR 0022 bổ sung).
/// <see cref="Items"/> chọn hoàn một phần theo từng thuốc/số lượng; rỗng hoặc null = hoàn toàn bộ (mặc định
/// cũ, giữ tương thích).
/// </summary>
public sealed record ReturnStockRequest(string Reason, IReadOnlyList<ReturnStockItemRequest>? Items = null);
