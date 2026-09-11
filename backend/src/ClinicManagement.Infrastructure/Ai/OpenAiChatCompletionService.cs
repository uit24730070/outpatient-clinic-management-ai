using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClinicManagement.Application.Common.Ai;
using ClinicManagement.Shared.Results;
using Microsoft.Extensions.Options;

namespace ClinicManagement.Infrastructure.Ai;

/// <summary>
/// Hiện thực <see cref="IChatCompletionService"/> gọi OpenAI Chat Completions API qua HTTP thuần
/// (<c>POST /v1/chat/completions</c>, model GPT-5 mini). Mọi lỗi mạng/parse được gói vào
/// <see cref="Error"/> mã <c>Ai.*</c>.
/// </summary>
/// <remarks>
/// Model GPT-5 dùng <c>max_completion_tokens</c> (không phải <c>max_tokens</c>), <b>không</b> nhận
/// <c>temperature</c> tuỳ biến (bỏ qua trường Temperature của yêu cầu trung lập), và nhận
/// <c>reasoning_effort</c> để điều chỉnh mức suy luận.
/// </remarks>
public sealed class OpenAiChatCompletionService : IChatCompletionService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly AiSettings _settings;

    public OpenAiChatCompletionService(HttpClient http, IOptions<AiSettings> settings)
    {
        _http = http;
        _settings = settings.Value;
    }

    public async Task<Result<ChatCompletionResult>> CompleteAsync(
        ChatCompletionRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            return Error.Failure("Ai.Unavailable", "Trợ lý AI chưa được cấu hình khoá API.");

        var messages = new List<OpenAiMessage>();
        if (!string.IsNullOrWhiteSpace(request.System))
            messages.Add(new OpenAiMessage { Role = "system", Content = request.System });
        messages.AddRange(request.Messages.Select(m => new OpenAiMessage
        {
            Role = m.Role == ChatRole.Assistant ? "assistant" : "user",
            Content = m.Content
        }));

        var payload = new OpenAiRequest
        {
            Model = _settings.Model,
            MaxCompletionTokens = request.MaxTokens ?? _settings.MaxTokens,
            ReasoningEffort = string.IsNullOrWhiteSpace(_settings.ReasoningEffort)
                ? null : _settings.ReasoningEffort,
            Messages = messages
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
        {
            Content = JsonContent.Create(payload, options: JsonOptions)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(httpRequest, ct);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return Error.Failure("Ai.Timeout", "Trợ lý AI phản hồi quá lâu, vui lòng thử lại.");
        }
        catch (HttpRequestException ex)
        {
            return Error.Failure("Ai.Unavailable", $"Không kết nối được tới dịch vụ AI: {ex.Message}");
        }

        if (!response.IsSuccessStatusCode)
            return Error.Failure("Ai.Unavailable",
                $"Dịch vụ AI trả về mã lỗi {(int)response.StatusCode}.");

        OpenAiResponse? parsed;
        try
        {
            parsed = await response.Content.ReadFromJsonAsync<OpenAiResponse>(JsonOptions, ct);
        }
        catch (JsonException)
        {
            return Error.Failure("Ai.BadResponse", "Không đọc được phản hồi từ dịch vụ AI.");
        }

        var text = parsed?.Choices?
            .Select(c => c.Message?.Content)
            .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t));

        if (string.IsNullOrWhiteSpace(text))
            return Error.Failure("Ai.BadResponse", "Phản hồi từ dịch vụ AI rỗng hoặc không hợp lệ.");

        return new ChatCompletionResult(text, parsed!.Model ?? _settings.Model);
    }

    // --- DTO khớp wire-format OpenAI Chat Completions API (chỉ ở Infrastructure) ---

    private sealed class OpenAiRequest
    {
        [JsonPropertyName("model")] public string Model { get; init; } = string.Empty;
        [JsonPropertyName("max_completion_tokens")] public int MaxCompletionTokens { get; init; }
        [JsonPropertyName("reasoning_effort")] public string? ReasoningEffort { get; init; }
        [JsonPropertyName("messages")] public List<OpenAiMessage> Messages { get; init; } = new();
    }

    private sealed class OpenAiMessage
    {
        [JsonPropertyName("role")] public string Role { get; init; } = string.Empty;
        [JsonPropertyName("content")] public string Content { get; init; } = string.Empty;
    }

    private sealed class OpenAiResponse
    {
        [JsonPropertyName("model")] public string? Model { get; init; }
        [JsonPropertyName("choices")] public List<OpenAiChoice>? Choices { get; init; }
    }

    private sealed class OpenAiChoice
    {
        [JsonPropertyName("message")] public OpenAiResponseMessage? Message { get; init; }
    }

    private sealed class OpenAiResponseMessage
    {
        [JsonPropertyName("content")] public string? Content { get; init; }
    }
}
