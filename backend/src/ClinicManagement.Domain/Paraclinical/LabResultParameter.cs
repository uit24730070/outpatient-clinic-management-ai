using System.Globalization;

namespace ClinicManagement.Domain.Paraclinical;

/// <summary>
/// Một thông số kết quả có cấu trúc (VD "WBC" = "7.2", đơn vị "10^9/L", tham chiếu "4.0-10.0") — dùng
/// cho mục chỉ định nhóm Xét nghiệm (<c>ParaclinicalGroup.LabTest</c>, ADR 0024) thay cho một khối văn
/// bản tự do <see cref="LabOrderItem.ResultText"/>. Owned bởi <see cref="LabOrderItem"/>, luôn <b>thay
/// toàn bộ</b> mỗi lần nhập lại kết quả (giống <see cref="LabOrder.ReplaceItems"/>) — không cần Id riêng.
/// </summary>
public class LabResultParameter
{
    // EF Core cần constructor không tham số.
    private LabResultParameter() { }

    private LabResultParameter(string name, string value, string? unit, string? referenceRange, bool isAbnormal)
    {
        Name = name;
        Value = value;
        Unit = unit;
        ReferenceRange = referenceRange;
        IsAbnormal = isAbnormal;
    }

    /// <summary>Tạo mới, tự tính <see cref="IsAbnormal"/> khi Giá trị và Khoảng tham chiếu đều đọc được
    /// dạng số ("thấp-cao"); đọc không được (định tính, VD "Âm tính") thì để <c>false</c> — không đoán.</summary>
    public static LabResultParameter Create(string name, string value, string? unit, string? referenceRange) =>
        new(name, value, unit, referenceRange, ComputeIsAbnormal(value, referenceRange));

    /// <summary>Tên thông số (VD "Bạch cầu", "Glucose").</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Giá trị đo được — văn bản tự do để chứa cả kết quả định tính (VD "Âm tính").</summary>
    public string Value { get; private set; } = null!;

    /// <summary>Đơn vị đo (tuỳ chọn, VD "10^9/L", "mmol/L").</summary>
    public string? Unit { get; private set; }

    /// <summary>Khoảng tham chiếu bình thường (tuỳ chọn) — dạng "thấp-cao" nếu muốn tự động gắn cờ bất
    /// thường, hoặc văn bản tự do (VD "Âm tính") chỉ để hiển thị.</summary>
    public string? ReferenceRange { get; private set; }

    /// <summary>Ngoài khoảng tham chiếu — chỉ tính được khi cả hai đọc ra số; không tự suy diễn được thì
    /// giữ <c>false</c> (kỹ thuật viên tự đọc bằng mắt qua cột Khoảng tham chiếu).</summary>
    public bool IsAbnormal { get; private set; }

    // Khoảng tham chiếu xét nghiệm luôn không âm trên thực tế nên tách bằng '-' là an toàn (không cần
    // xử lý số âm ở vế trái, tránh nhầm dấu trừ của số âm với dấu phân cách).
    private static bool ComputeIsAbnormal(string value, string? referenceRange)
    {
        if (string.IsNullOrWhiteSpace(referenceRange)) return false;
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var v)) return false;

        var parts = referenceRange.Split('-', StringSplitOptions.TrimEntries);
        if (parts.Length != 2) return false;
        if (!decimal.TryParse(parts[0], NumberStyles.Number, CultureInfo.InvariantCulture, out var low)) return false;
        if (!decimal.TryParse(parts[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var high)) return false;

        return v < low || v > high;
    }
}
