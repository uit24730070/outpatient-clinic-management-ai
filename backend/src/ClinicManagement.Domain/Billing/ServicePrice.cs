using ClinicManagement.Domain.Common;

namespace ClinicManagement.Domain.Billing;

/// <summary>
/// Một mục trong bảng giá dịch vụ (công khám, thủ thuật, tư vấn…). <see cref="Code"/> (mã DV-)
/// là định danh nghiệp vụ duy nhất. Dùng làm nguồn dựng dòng hoá đơn; giá được <b>snapshot</b>
/// sang <c>InvoiceItem</c> lúc lập hoá đơn nên đổi giá sau không ảnh hưởng hoá đơn cũ.
/// </summary>
public class ServicePrice : Entity
{
    // EF Core cần constructor không tham số.
    private ServicePrice() { }

    public ServicePrice(
        string code, string name, decimal unitPrice, string? description,
        ServiceCategory category = ServiceCategory.Other)
    {
        Code = code;
        Name = name;
        UnitPrice = unitPrice;
        Description = description;
        Category = category;
    }

    /// <summary>Mã dịch vụ duy nhất, ví dụ DV-000001.</summary>
    public string Code { get; private set; } = null!;

    /// <summary>Tên dịch vụ.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Đơn giá (VND), làm tròn về đồng. Không âm.</summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>Mô tả thêm (tuỳ chọn).</summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Phân loại dịch vụ (công khám/cận lâm sàng/khác). Mặc định <see cref="ServiceCategory.Other"/>
    /// để tương thích dữ liệu Sprint 14 (ADR 0015).
    /// </summary>
    public ServiceCategory Category { get; private set; }

    /// <summary>Cập nhật các trường có thể chỉnh sửa của mục bảng giá.</summary>
    public void UpdateDetails(
        string name, decimal unitPrice, string? description,
        ServiceCategory category = ServiceCategory.Other)
    {
        Name = name;
        UnitPrice = unitPrice;
        Description = description;
        Category = category;
    }
}
