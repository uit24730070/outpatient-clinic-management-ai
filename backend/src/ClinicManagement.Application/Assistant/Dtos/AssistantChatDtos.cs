namespace ClinicManagement.Application.Assistant.Dtos;

/// <summary>Một lượt hội thoại do client gửi lên. <paramref name="Role"/>: <c>"user"</c> hoặc <c>"assistant"</c>.</summary>
public sealed record AssistantMessageDto(string Role, string Content);

/// <summary>Yêu cầu trò chuyện: toàn bộ lịch sử hội thoại (multi-turn). Lượt cuối phải là của người dùng.</summary>
public sealed record AssistantChatRequest(IReadOnlyList<AssistantMessageDto> Messages);

/// <summary>Một lần gọi công cụ đã thực hiện (để truy vết): tên, tham số, và kết quả rút gọn.</summary>
public sealed record AssistantToolCallDto(string Name, string Arguments, string Result);

/// <summary>
/// Kết quả trò chuyện: câu trả lời cuối, model đã dùng, và danh sách công cụ đã gọi (traceability).
/// </summary>
public sealed record AssistantReplyDto(
    string Answer,
    string Model,
    IReadOnlyList<AssistantToolCallDto> ToolCalls,
    DateTimeOffset GeneratedAt);
