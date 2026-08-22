using ClinicManagement.Domain.Common;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Domain.Pharmacy;

/// <summary>
/// Lô thuốc: đơn vị tồn kho theo <b>lô + hạn dùng</b> của một thuốc (<see cref="MedicationId"/>).
/// Tồn của lô ở <see cref="QuantityOnHand"/>. Lô <b>chỉ sinh/tăng qua phiếu nhập</b> ở P1
/// (không CRUD trực tiếp). Nền cho cấp phát FEFO (first-expired-first-out) ở P2 — xem ADR 0011.
/// </summary>
public class MedicationBatch : Entity
{
    // EF Core cần constructor không tham số.
    private MedicationBatch() { }

    public MedicationBatch(Guid medicationId, string batchNumber, DateOnly expiryDate, int quantityOnHand)
    {
        MedicationId = medicationId;
        BatchNumber = batchNumber;
        ExpiryDate = expiryDate;
        QuantityOnHand = quantityOnHand;
    }

    /// <summary>Thuốc mà lô này thuộc về.</summary>
    public Guid MedicationId { get; private set; }

    /// <summary>Số lô của nhà sản xuất.</summary>
    public string BatchNumber { get; private set; } = null!;

    /// <summary>Hạn dùng (chỉ cần ngày).</summary>
    public DateOnly ExpiryDate { get; private set; }

    /// <summary>Số lượng tồn hiện tại của lô (≥ 0).</summary>
    public int QuantityOnHand { get; private set; }

    /// <summary>Cộng thêm tồn cho lô (dùng khi nhập kho). Số lượng phải &gt; 0.</summary>
    public Result Increase(int quantity)
    {
        if (quantity <= 0)
            return Result.Failure(Error.Validation(
                "MedicationBatch.InvalidQuantity",
                "Số lượng nhập phải lớn hơn 0."));

        QuantityOnHand += quantity;
        return Result.Success();
    }
}
