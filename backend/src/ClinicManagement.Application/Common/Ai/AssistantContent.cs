namespace ClinicManagement.Application.Common.Ai;

/// <summary>Vai trò một lượt hội thoại trợ lý (trung lập provider).</summary>
public enum AssistantRole
{
    User,
    Assistant
}

/// <summary>
/// Một khối nội dung trong hội thoại trợ lý — có thể là văn bản, một yêu cầu gọi công cụ
/// (do LLM sinh ra), hoặc kết quả trả về của công cụ. Trung lập provider.
/// </summary>
public abstract record AssistantContent;

/// <summary>Khối văn bản thuần.</summary>
public sealed record AssistantText(string Text) : AssistantContent;

/// <summary>
/// LLM yêu cầu gọi một công cụ. <paramref name="Id"/> là định danh do provider cấp (đối chiếu
/// kết quả), <paramref name="ArgumentsJson"/> là tham số dạng JSON (công cụ tự phân giải).
/// </summary>
public sealed record AssistantToolUse(string Id, string Name, string ArgumentsJson) : AssistantContent;

/// <summary>Kết quả thực thi công cụ, gửi lại cho LLM (đối chiếu theo <paramref name="ToolUseId"/>).</summary>
public sealed record AssistantToolResult(string ToolUseId, string Content, bool IsError = false) : AssistantContent;

/// <summary>Một lượt hội thoại: vai trò + danh sách khối nội dung.</summary>
public sealed record AssistantMessage(AssistantRole Role, IReadOnlyList<AssistantContent> Content);
