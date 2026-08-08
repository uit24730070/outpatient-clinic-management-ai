namespace ClinicManagement.Domain.Common;

/// <summary>
/// Đánh dấu thực thể hỗ trợ xoá mềm (soft delete): bản ghi bị ẩn thay vì xoá vật lý.
/// Được dùng để áp global query filter tự động trong <c>AppDbContext</c>.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTimeOffset? DeletedAt { get; }
}
