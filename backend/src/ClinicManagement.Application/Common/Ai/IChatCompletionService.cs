using ClinicManagement.Shared.Results;

namespace ClinicManagement.Application.Common.Ai;

/// <summary>
/// Trừu tượng gọi LLM (chat completion) — Application/Domain chỉ phụ thuộc interface này,
/// không biết provider cụ thể. Hiện thực (HTTP client thật hoặc fake) nằm ở Infrastructure.
/// Lỗi được gói vào <see cref="Error"/> với mã <c>Ai.*</c> thay vì ném exception.
/// </summary>
public interface IChatCompletionService
{
    Task<Result<ChatCompletionResult>> CompleteAsync(
        ChatCompletionRequest request, CancellationToken ct = default);
}
