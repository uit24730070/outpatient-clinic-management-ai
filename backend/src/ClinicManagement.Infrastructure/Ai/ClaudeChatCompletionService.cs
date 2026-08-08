using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClinicManagement.Application.Common.Ai;
using ClinicManagement.Shared.Results;
using Microsoft.Extensions.Options;

namespace ClinicManagement.Infrastructure.Ai;

/// <summary>
/// Hiện thực <see cref="IChatCompletionService"/> gọi Claude API (Anthropic) qua HTTP thuần
/// (<c>POST /v1/messages</c>). Mọi lỗi mạng/parse được gói vào <see cref="Error"/> mã <c>Ai.*</c>.
/// </summary>
public sealed class ClaudeChatCompletionService : IChatCompletionService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly AiSettings _settings;

    public ClaudeChatCompletionService(HttpClient http, IOptions<AiSettings> settings)
    {
        _http = http;
        _settings = settings.Value;
    }

    public async Task<Result<ChatCompletionResult>> CompleteAsync(
        ChatCompletionRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            return Error.Failure("Ai.Unavailable", "Trợ lý AI chưa được cấu hình khoá API.");

        var payload = new ClaudeRequest
        {
            Model = _settings.Model,
            MaxTokens = request.MaxTokens ?? _settings.MaxTokens,
            System = request.System,
            Temperature = request.Temperature,
            Messages = request.Messages
                .Select(m => new ClaudeMessage
                {
                    Role = m.Role == ChatRole.Assistant ? "assistant" : "user",
                    Content = m.Content
                })
                .ToList()
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/messages")
        {
            Content = JsonContent.Create(payload, options: JsonOptions)
        };
        httpRequest.Headers.Add("x-api-key", _settings.ApiKey);
        httpRequest.Headers.Add("anthropic-version", _settings.AnthropicVersion);

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

        ClaudeResponse? parsed;
        try
        {
            parsed = await response.Content.ReadFromJsonAsync<ClaudeResponse>(JsonOptions, ct);
        }
        catch (JsonException)
        {
            return Error.Failure("Ai.BadResponse", "Không đọc được phản hồi từ dịch vụ AI.");
        }

        var text = parsed?.Content?
            .Where(c => c.Type == "text")
            .Select(c => c.Text)
            .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t));

        if (string.IsNullOrWhiteSpace(text))
            return Error.Failure("Ai.BadResponse", "Phản hồi từ dịch vụ AI rỗng hoặc không hợp lệ.");

        return new ChatCompletionResult(text, parsed!.Model ?? _settings.Model);
    }

    // --- DTO khớp wire-format Anthropic Messages API (chỉ ở Infrastructure) ---

    private sealed class ClaudeRequest
    {
        [JsonPropertyName("model")] public string Model { get; init; } = string.Empty;
        [JsonPropertyName("max_tokens")] public int MaxTokens { get; init; }
        [JsonPropertyName("system")] public string? System { get; init; }
        [JsonPropertyName("temperature")] public double? Temperature { get; init; }
        [JsonPropertyName("messages")] public List<ClaudeMessage> Messages { get; init; } = new();
    }

    private sealed class ClaudeMessage
    {
        [JsonPropertyName("role")] public string Role { get; init; } = string.Empty;
        [JsonPropertyName("content")] public string Content { get; init; } = string.Empty;
    }

    private sealed class ClaudeResponse
    {
        [JsonPropertyName("model")] public string? Model { get; init; }
        [JsonPropertyName("content")] public List<ClaudeContentBlock>? Content { get; init; }
    }

    private sealed class ClaudeContentBlock
    {
        [JsonPropertyName("type")] public string? Type { get; init; }
        [JsonPropertyName("text")] public string? Text { get; init; }
    }
}
