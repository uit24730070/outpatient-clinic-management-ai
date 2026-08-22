using ClinicManagement.Domain.Common;

namespace ClinicManagement.Domain.Pharmacy;

/// <summary>
/// Thuốc trong danh mục kho dược. Mã thuốc (<see cref="Code"/>) là định danh nghiệp vụ duy nhất.
/// Là <b>aggregate root</b> của các lô thuốc (<see cref="MedicationBatch"/>); tồn tổng của thuốc
/// bằng tổng <c>QuantityOnHand</c> các lô chưa xoá (tính phía server, không nạp về bộ nhớ).
/// </summary>
public class Medication : Entity
{
    // EF Core cần constructor không tham số.
    private Medication() { }

    public Medication(
        string code,
        string name,
        string activeIngredient,
        string unit,
        int reorderLevel,
        string? description)
    {
        Code = code;
        Name = name;
        ActiveIngredient = activeIngredient;
        Unit = unit;
        ReorderLevel = reorderLevel;
        Description = description;
    }

    /// <summary>Mã thuốc duy nhất, ví dụ TH-000001.</summary>
    public string Code { get; private set; } = null!;

    /// <summary>Tên thương mại của thuốc.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Hoạt chất (ví dụ "Paracetamol").</summary>
    public string ActiveIngredient { get; private set; } = null!;

    /// <summary>Đơn vị tính (viên/vỉ/chai/ống…), văn bản tự do.</summary>
    public string Unit { get; private set; } = null!;

    /// <summary>Ngưỡng tồn tối thiểu để cảnh báo (dùng ở P2). Mặc định 0.</summary>
    public int ReorderLevel { get; private set; }

    /// <summary>Mô tả thêm (tuỳ chọn).</summary>
    public string? Description { get; private set; }

    /// <summary>Cập nhật các trường có thể chỉnh sửa của thuốc.</summary>
    public void UpdateDetails(
        string name,
        string activeIngredient,
        string unit,
        int reorderLevel,
        string? description)
    {
        Name = name;
        ActiveIngredient = activeIngredient;
        Unit = unit;
        ReorderLevel = reorderLevel;
        Description = description;
    }
}
