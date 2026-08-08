namespace ClinicManagement.Domain.Common;

/// <summary>
/// Lớp cơ sở cho mọi thực thể: khóa chính <see cref="Guid"/> và dấu thời gian kiểm toán.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
