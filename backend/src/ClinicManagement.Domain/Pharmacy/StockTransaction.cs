using ClinicManagement.Domain.Common;

namespace ClinicManagement.Domain.Pharmacy;

/// <summary>
/// Bản ghi <b>bất biến</b> trong sổ cái giao dịch tồn: mọi thay đổi tồn của một lô
/// (<see cref="MedicationBatchId"/>) đều để lại một dòng ở đây để truy vết (ai/khi nào/tham chiếu nào).
/// P1 chỉ ghi <see cref="StockTransactionType.Import"/> khi nhập kho; nền cho báo cáo xuất–nhập–tồn
/// và cấp phát <see cref="StockTransactionType.Dispense"/> ở P2 (ADR 0011).
/// </summary>
public class StockTransaction : Entity
{
    // EF Core cần constructor không tham số.
    private StockTransaction() { }

    public StockTransaction(
        Guid medicationBatchId,
        StockTransactionType type,
        int quantityDelta,
        string referenceType,
        Guid referenceId,
        DateTimeOffset occurredAt)
    {
        MedicationBatchId = medicationBatchId;
        Type = type;
        QuantityDelta = quantityDelta;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        OccurredAt = occurredAt;
    }

    /// <summary>Lô thuốc bị ảnh hưởng.</summary>
    public Guid MedicationBatchId { get; private set; }

    /// <summary>Loại giao dịch (Import/Dispense/Adjust).</summary>
    public StockTransactionType Type { get; private set; }

    /// <summary>Chênh lệch tồn: dương = nhập, âm = xuất.</summary>
    public int QuantityDelta { get; private set; }

    /// <summary>Loại chứng từ nguồn, ví dụ "StockReceipt".</summary>
    public string ReferenceType { get; private set; } = null!;

    /// <summary>Id chứng từ nguồn (trỏ phiếu nhập/đơn cấp phát).</summary>
    public Guid ReferenceId { get; private set; }

    /// <summary>Thời điểm phát sinh giao dịch.</summary>
    public DateTimeOffset OccurredAt { get; private set; }
}
