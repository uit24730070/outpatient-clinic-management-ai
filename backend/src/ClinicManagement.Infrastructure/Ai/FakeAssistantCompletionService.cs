using System.Text;
using ClinicManagement.Application.Common.Ai;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Infrastructure.Ai;

/// <summary>
/// Hiện thực fake của <see cref="IAssistantCompletionService"/> — trả câu trả lời tất định, không gọi
/// mạng và <b>không</b> yêu cầu gọi công cụ (tránh vòng lặp). Dùng khi thiếu khoá hoặc bật <c>Ai:UseFake</c>.
/// </summary>
public sealed class FakeAssistantCompletionService : IAssistantCompletionService
{
    public const string ModelName = "fake";

    public Task<Result<AssistantCompletionResult>> CompleteAsync(
        AssistantCompletionRequest request, CancellationToken ct = default)
    {
        // Lấy câu hỏi mới nhất của người dùng (khối văn bản cuối cùng có vai trò User).
        var lastUserText = request.Messages
            .Where(m => m.Role == AssistantRole.User)
            .SelectMany(m => m.Content)
            .OfType<AssistantText>()
            .LastOrDefault()?.Text ?? string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("**[Trợ lý mô phỏng — chế độ AI fake]**").AppendLine();
        sb.Append("Bạn vừa hỏi: \"").Append(lastUserText.Trim()).AppendLine("\".").AppendLine();
        sb.Append("Hệ thống đang chạy ở chế độ giả lập (không gọi LLM thật), có ")
          .Append(request.Tools.Count)
          .AppendLine(" công cụ nghiệp vụ khả dụng.");
        sb.AppendLine("Khi cấu hình khoá API thật và tắt `Ai:UseFake`, trợ lý sẽ tự truy vấn dữ liệu " +
                      "qua công cụ để trả lời. Đây là thông tin hỗ trợ, cần kiểm chứng.");

        var content = new AssistantContent[] { new AssistantText(sb.ToString().TrimEnd()) };
        Result<AssistantCompletionResult> result =
            new AssistantCompletionResult(content, StopIsToolUse: false, ModelName);
        return Task.FromResult(result);
    }
}
