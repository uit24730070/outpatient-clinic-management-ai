using ClinicManagement.Application.Common.Ai;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Infrastructure.Ai;

/// <summary>
/// Hiện thực fake của <see cref="IChatCompletionService"/> — trả tóm tắt tất định, không gọi mạng.
/// Dùng khi thiếu khoá hoặc bật <c>Ai:UseFake</c> (dev/test không tốn phí).
/// </summary>
public sealed class FakeChatCompletionService : IChatCompletionService
{
    public const string ModelName = "fake";

    public Task<Result<ChatCompletionResult>> CompleteAsync(
        ChatCompletionRequest request, CancellationToken ct = default)
    {
        var prompt = request.Messages.LastOrDefault()?.Content ?? string.Empty;
        var lineCount = prompt.Split('\n').Length;

        var text =
            "**[Bản tóm tắt mô phỏng — chế độ AI fake]**\n\n" +
            "Đây là tóm tắt tất định do chế độ giả lập sinh ra (không gọi LLM thật). " +
            $"Ngữ cảnh nhận được dài khoảng {prompt.Length} ký tự / {lineCount} dòng.\n\n" +
            "Khi cấu hình khoá API thật và tắt `Ai:UseFake`, hệ thống sẽ trả bản tóm tắt " +
            "lâm sàng thực tế từ dữ liệu bệnh án. Đây là thông tin tham khảo, không thay thế " +
            "đánh giá của bác sĩ.";

        Result<ChatCompletionResult> result = new ChatCompletionResult(text, ModelName);
        return Task.FromResult(result);
    }
}
