namespace ClinicManagement.Domain.Encounters;

/// <summary>
/// Một dòng đơn thuốc thuộc phiếu khám (<see cref="Encounter"/>). Là <b>owned entity</b> —
/// không có vòng đời độc lập, luôn được tạo/sửa/đọc theo cả cụm cùng phiếu (xem ADR 0006).
/// <para>
/// Từ P2 (ADR 0011) có thể <b>tuỳ chọn</b> gắn với một thuốc trong danh mục qua
/// <see cref="MedicationId"/>: khi có, dòng đơn được cấp phát trừ tồn theo FEFO lúc chốt phiếu.
/// Vẫn cho phép kê thuốc ngoài danh mục (MedicationId = null) — <see cref="DrugName"/> luôn là
/// văn bản hiển thị, giữ tương thích ngược với đơn cũ.
/// </para>
/// </summary>
public class PrescriptionItem
{
    // EF Core cần constructor không tham số.
    private PrescriptionItem() { }

    public PrescriptionItem(string drugName, string dosage, int quantity, string? instruction, Guid? medicationId = null)
    {
        DrugName = drugName;
        Dosage = dosage;
        Quantity = quantity;
        Instruction = instruction;
        MedicationId = medicationId;
    }

    /// <summary>
    /// Thuốc trong danh mục mà dòng đơn tham chiếu (tuỳ chọn). <c>null</c> = thuốc ngoài danh mục
    /// (không trừ tồn). Có giá trị = cấp phát trừ tồn FEFO khi chốt phiếu (ADR 0011).
    /// </summary>
    public Guid? MedicationId { get; private set; }

    /// <summary>Tên thuốc (văn bản tự do; điền từ danh mục khi có <see cref="MedicationId"/>).</summary>
    public string DrugName { get; private set; } = null!;

    /// <summary>Liều dùng, ví dụ "500mg".</summary>
    public string Dosage { get; private set; } = null!;

    /// <summary>Số lượng (viên/gói/...), phải > 0.</summary>
    public int Quantity { get; private set; }

    /// <summary>Cách dùng, ví dụ "Ngày 2 lần sau ăn" (tuỳ chọn).</summary>
    public string? Instruction { get; private set; }
}
