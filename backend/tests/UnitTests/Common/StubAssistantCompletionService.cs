using ClinicManagement.Application.Common.Ai;
using ClinicManagement.Shared.Results;

namespace UnitTests.Common;

/// <summary>
/// Stub <see cref="IAssistantCompletionService"/> — trả lần lượt các kết quả cấu hình sẵn (không gọi
/// mạng). Ghi lại số lần gọi và request cuối để kiểm chứng vòng lặp tool-calling.
/// </summary>
public sealed class StubAssistantCompletionService : IAssistantCompletionService
{
    private readonly Queue<Result<AssistantCompletionResult>> _queue;
    private readonly Result<AssistantCompletionResult>? _repeat;

    public int Calls { get; private set; }
    public AssistantCompletionRequest? LastRequest { get; private set; }

    private StubAssistantCompletionService(
        IEnumerable<Result<AssistantCompletionResult>> results,
        Result<AssistantCompletionResult>? repeat)
    {
        _queue = new Queue<Result<AssistantCompletionResult>>(results);
        _repeat = repeat;
    }

    /// <summary>Trả lần lượt các kết quả; hết hàng đợi sẽ ném (kiểm soát chặt số vòng).</summary>
    public static StubAssistantCompletionService Sequence(params Result<AssistantCompletionResult>[] results) =>
        new(results, null);

    /// <summary>Luôn trả cùng một kết quả (vd luôn yêu cầu gọi công cụ để thử chặn vượt vòng).</summary>
    public static StubAssistantCompletionService Always(Result<AssistantCompletionResult> result) =>
        new(Array.Empty<Result<AssistantCompletionResult>>(), result);

    public Task<Result<AssistantCompletionResult>> CompleteAsync(
        AssistantCompletionRequest request, CancellationToken ct = default)
    {
        Calls++;
        LastRequest = request;
        var result = _queue.Count > 0 ? _queue.Dequeue()
            : _repeat ?? throw new InvalidOperationException("StubAssistant: hết kết quả cấu hình sẵn.");
        return Task.FromResult(result);
    }

    // --- Tiện ích dựng kết quả ---

    public static Result<AssistantCompletionResult> Text(string text, string model = "test-model") =>
        new AssistantCompletionResult(new AssistantContent[] { new AssistantText(text) }, false, model);

    public static Result<AssistantCompletionResult> ToolUse(
        string id, string name, string argumentsJson, string model = "test-model") =>
        new AssistantCompletionResult(
            new AssistantContent[] { new AssistantToolUse(id, name, argumentsJson) }, true, model);
}
