using ClinicManagement.Shared.Results;

namespace ClinicManagement.Application.Common.Ai;

/// <summary>
/// Yêu cầu một lượt sinh của LLM có hỗ trợ tool-use. Gửi kèm lịch sử hội thoại (đã gồm
/// các khối tool-use/tool-result nếu có) và danh sách công cụ khả dụng.
/// </summary>
public sealed record AssistantCompletionRequest(
    IReadOnlyList<AssistantMessage> Messages,
    IReadOnlyList<AiTool> Tools,
    string? System = null,
    int? MaxTokens = null);

/// <summary>
/// Kết quả một lượt sinh: danh sách khối nội dung (văn bản và/hoặc yêu cầu gọi công cụ),
/// cờ <paramref name="StopIsToolUse"/> cho biết LLM đang chờ hệ thống thực thi công cụ, và model đã dùng.
/// </summary>
public sealed record AssistantCompletionResult(
    IReadOnlyList<AssistantContent> Content,
    bool StopIsToolUse,
    string Model);

/// <summary>
/// Trừu tượng gọi LLM ở mức "một lượt" có tool-use (provider-neutral). Vòng lặp gọi công cụ
/// (orchestration) nằm ở <see cref="ClinicManagement.Application.Assistant.IAssistantService"/>.
/// Hiện thực (Claude thật hoặc fake) ở Infrastructure. Lỗi gói vào <see cref="Error"/> mã <c>Ai.*</c>.
/// </summary>
public interface IAssistantCompletionService
{
    Task<Result<AssistantCompletionResult>> CompleteAsync(
        AssistantCompletionRequest request, CancellationToken ct = default);
}
