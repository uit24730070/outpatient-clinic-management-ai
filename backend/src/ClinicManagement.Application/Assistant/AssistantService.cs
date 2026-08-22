using ClinicManagement.Application.Assistant.Dtos;
using ClinicManagement.Application.Common.Ai;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Application.Assistant;

/// <summary>
/// Điều phối hội thoại có tool-calling (AI-03): gửi lịch sử + danh sách công cụ tới LLM; khi LLM yêu
/// cầu gọi công cụ thì thực thi qua registry (tôn trọng RBAC) rồi lặp lại tới khi có câu trả lời cuối.
/// Giới hạn <see cref="MaxToolRounds"/> vòng để tránh lặp vô hạn / chi phí phình to.
/// </summary>
public sealed class AssistantService : IAssistantService
{
    /// <summary>Số vòng gọi công cụ tối đa trước khi dừng (mỗi vòng có thể gồm nhiều công cụ).</summary>
    private const int MaxToolRounds = 5;

    private const string SystemPrompt =
        "Bạn là trợ lý ảo cho nhân viên một phòng khám ngoại trú (lễ tân, bác sĩ, quản trị). " +
        "Trả lời bằng tiếng Việt, ngắn gọn, chính xác. " +
        "Chỉ dùng dữ liệu lấy được qua các công cụ được cung cấp — KHÔNG bịa thông tin. " +
        "Khi cần dữ liệu (bệnh nhân, lịch khám, bác sĩ, phiếu khám) hãy gọi công cụ phù hợp; " +
        "có thể gọi nhiều công cụ nối tiếp (vd tìm Id bệnh nhân trước rồi mới tra lịch sử khám). " +
        "Nếu công cụ trả về rỗng hoặc báo lỗi, hãy nói rõ là chưa có/không truy được dữ liệu. " +
        "Không đưa chẩn đoán mới hay chỉ định điều trị. Đây là thông tin hỗ trợ, cần người dùng kiểm chứng.";

    private readonly IAssistantCompletionService _assistant;
    private readonly IReadOnlyDictionary<string, IAssistantTool> _tools;
    private readonly IReadOnlyList<AiTool> _toolDefinitions;

    public AssistantService(IAssistantCompletionService assistant, IEnumerable<IAssistantTool> tools)
    {
        _assistant = assistant;
        var list = tools.ToList();
        _tools = list.ToDictionary(t => t.Definition.Name, StringComparer.Ordinal);
        _toolDefinitions = list.Select(t => t.Definition).ToList();
    }

    public async Task<Result<AssistantReplyDto>> ChatAsync(
        AssistantChatRequest request, AssistantContext context, CancellationToken ct = default)
    {
        var incoming = request.Messages ?? Array.Empty<AssistantMessageDto>();

        // Chuẩn hoá: bỏ lượt rỗng; hội thoại phải có nội dung và kết thúc bằng lượt người dùng.
        var conversation = new List<AssistantMessage>();
        foreach (var m in incoming)
        {
            if (string.IsNullOrWhiteSpace(m.Content)) continue;
            var role = string.Equals(m.Role, "assistant", StringComparison.OrdinalIgnoreCase)
                ? AssistantRole.Assistant
                : AssistantRole.User;
            conversation.Add(new AssistantMessage(role,
                new AssistantContent[] { new AssistantText(m.Content.Trim()) }));
        }

        if (conversation.Count == 0 || conversation[^1].Role != AssistantRole.User)
            return Error.Validation("Ai.QuestionRequired", "Vui lòng nhập câu hỏi.");

        var toolCalls = new List<AssistantToolCallDto>();
        var model = "-";

        for (var round = 0; ; round++)
        {
            var completion = await _assistant.CompleteAsync(
                new AssistantCompletionRequest(conversation, _toolDefinitions, System: SystemPrompt), ct);
            if (completion.IsFailure)
                return Result.Failure<AssistantReplyDto>(completion.Error);

            model = completion.Value.Model;
            var content = completion.Value.Content;
            var toolUses = content.OfType<AssistantToolUse>().ToList();

            // Ghi lại lượt trả lời của trợ lý (gồm cả yêu cầu gọi công cụ) vào hội thoại.
            conversation.Add(new AssistantMessage(AssistantRole.Assistant, content));

            // Không còn yêu cầu công cụ → đây là câu trả lời cuối.
            if (toolUses.Count == 0)
            {
                var answer = string.Join("\n",
                    content.OfType<AssistantText>().Select(t => t.Text)).Trim();
                if (answer.Length == 0)
                    answer = "Xin lỗi, tôi chưa tạo được câu trả lời phù hợp.";
                return new AssistantReplyDto(answer, model, toolCalls, DateTimeOffset.UtcNow);
            }

            // Đã đạt giới hạn vòng mà LLM vẫn muốn gọi công cụ → dừng để tránh lặp vô hạn.
            if (round >= MaxToolRounds)
                return Error.Failure("Ai.ToolLoopExceeded",
                    "Trợ lý cần quá nhiều bước để trả lời. Vui lòng đặt câu hỏi cụ thể hơn.");

            // Thực thi từng công cụ (tôn trọng RBAC qua context) và gom kết quả trả lại LLM.
            var toolResults = new List<AssistantContent>(toolUses.Count);
            foreach (var use in toolUses)
            {
                string output;
                if (_tools.TryGetValue(use.Name, out var tool))
                {
                    try
                    {
                        output = await tool.ExecuteAsync(use.ArgumentsJson, context, ct);
                    }
                    catch (Exception ex)
                    {
                        output = ToolJsonError($"Lỗi khi thực thi công cụ: {ex.Message}");
                    }
                }
                else
                {
                    output = ToolJsonError($"Không có công cụ tên '{use.Name}'.");
                }

                toolCalls.Add(new AssistantToolCallDto(use.Name, use.ArgumentsJson, output));
                toolResults.Add(new AssistantToolResult(use.Id, output));
            }

            conversation.Add(new AssistantMessage(AssistantRole.User, toolResults));
        }
    }

    private static string ToolJsonError(string message) => Tools.ToolJson.Error(message);
}
