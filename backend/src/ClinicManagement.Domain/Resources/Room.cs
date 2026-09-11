using ClinicManagement.Domain.Common;

namespace ClinicManagement.Domain.Resources;

/// <summary>
/// Phòng khám (tài nguyên vật lý). Mã phòng (<see cref="Code"/>) là định danh nghiệp vụ duy nhất
/// (dạng PK-000001). Dùng <b>soft delete</b> để ngừng sử dụng phòng — không có cờ <c>IsActive</c>
/// riêng (tránh lặp hai cờ vòng đời như <c>User</c>).
/// </summary>
public class Room : Entity
{
    // EF Core cần constructor không tham số.
    private Room() { }

    public Room(string code, string name, string? description)
    {
        Code = code;
        Name = name;
        Description = description;
    }

    /// <summary>Mã phòng duy nhất, ví dụ PK-000001.</summary>
    public string Code { get; private set; } = null!;

    /// <summary>Tên/số hiệu phòng (ví dụ "Phòng 101 - Nội tổng quát").</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Mô tả thêm (tuỳ chọn).</summary>
    public string? Description { get; private set; }

    /// <summary>Cập nhật thông tin phòng.</summary>
    public void UpdateDetails(string name, string? description)
    {
        Name = name;
        Description = description;
    }
}
