namespace ClinicManagement.Domain.Common;

/// <summary>
/// Lớp cơ sở cho mọi thực thể: khóa chính <see cref="Guid"/>, dấu thời gian kiểm toán
/// và hỗ trợ xoá mềm (<see cref="ISoftDeletable"/>).
/// </summary>
public abstract class Entity : ISoftDeletable
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>Đã bị xoá mềm hay chưa. Bản ghi đã xoá bị ẩn khỏi mọi truy vấn mặc định.</summary>
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>Đánh dấu bản ghi đã bị xoá mềm. Không tác dụng nếu đã xoá trước đó.</summary>
    public void MarkAsDeleted()
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
    }
}
