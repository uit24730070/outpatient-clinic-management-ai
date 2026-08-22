namespace ClinicManagement.Application.Common.Ai;

/// <summary>
/// Ngữ cảnh người dùng đang trò chuyện — dùng để <b>kiểm soát quyền khi thực thi công cụ</b>
/// (không vượt RBAC). <paramref name="DoctorId"/> có giá trị nếu người dùng là bác sĩ đã gắn hồ sơ.
/// </summary>
public sealed record AssistantContext(Guid UserId, string Role, Guid? DoctorId)
{
    public bool IsDoctor => string.Equals(Role, "Doctor", StringComparison.OrdinalIgnoreCase);
    public bool IsAdmin => string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase);
    public bool IsReceptionist => string.Equals(Role, "Receptionist", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Một công cụ chỉ-đọc mà trợ lý có thể gọi. Bọc service nghiệp vụ sẵn có, trả kết quả gọn
/// (thường JSON) cho LLM. Thực thi phải tôn trọng RBAC theo <see cref="AssistantContext"/>.
/// </summary>
public interface IAssistantTool
{
    /// <summary>Định nghĩa trung lập provider (tên + mô tả + tham số) để chào cho LLM.</summary>
    AiTool Definition { get; }

    /// <summary>Thực thi công cụ với tham số JSON; trả chuỗi kết quả gọn để đưa lại LLM.</summary>
    Task<string> ExecuteAsync(string argumentsJson, AssistantContext context, CancellationToken ct = default);
}
