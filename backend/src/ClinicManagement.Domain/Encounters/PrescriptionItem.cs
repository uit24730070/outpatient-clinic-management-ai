namespace ClinicManagement.Domain.Encounters;

/// <summary>
/// Một dòng đơn thuốc thuộc phiếu khám (<see cref="Encounter"/>). Là <b>owned entity</b> —
/// không có vòng đời độc lập, luôn được tạo/sửa/đọc theo cả cụm cùng phiếu (xem ADR 0006).
/// Thuốc nhập văn bản tự do ở quy mô đồ án (chưa có danh mục thuốc chuẩn hoá).
/// </summary>
public class PrescriptionItem
{
    // EF Core cần constructor không tham số.
    private PrescriptionItem() { }

    public PrescriptionItem(string drugName, string dosage, int quantity, string? instruction)
    {
        DrugName = drugName;
        Dosage = dosage;
        Quantity = quantity;
        Instruction = instruction;
    }

    /// <summary>Tên thuốc (văn bản tự do).</summary>
    public string DrugName { get; private set; } = null!;

    /// <summary>Liều dùng, ví dụ "500mg".</summary>
    public string Dosage { get; private set; } = null!;

    /// <summary>Số lượng (viên/gói/...), phải > 0.</summary>
    public int Quantity { get; private set; }

    /// <summary>Cách dùng, ví dụ "Ngày 2 lần sau ăn" (tuỳ chọn).</summary>
    public string? Instruction { get; private set; }
}
