using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ClinicManagement.Application.Common.Ai;
using ClinicManagement.Shared.Results;
using Microsoft.Extensions.Options;

namespace ClinicManagement.Infrastructure.Ai;

/// <summary>
/// Hiện thực <see cref="IAssistantCompletionService"/> gọi Claude Messages API (Anthropic) với
/// <b>tool use</b> qua HTTP thuần (<c>POST /v1/messages</c>). Dịch DTO công cụ trung lập sang
/// <c>input_schema</c> (JSON Schema) và phân giải các khối <c>tool_use</c>/<c>text</c> trong phản hồi.
/// Mọi lỗi mạng/parse gói vào <see cref="Error"/> mã <c>Ai.*</c>.
/// </summary>
public sealed class ClaudeAssistantCompletionService : IAssistantCompletionService
{
    private readonly HttpClient _http;
    private readonly AiSettings _settings;

    public ClaudeAssistantCompletionService(HttpClient http, IOptions<AiSettings> settings)
    {
        _http = http;
        _settings = settings.Value;
    }

    public async Task<Result<AssistantCompletionResult>> CompleteAsync(
        AssistantCompletionRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            return Error.Failure("Ai.Unavailable", "Trợ lý AI chưa được cấu hình khoá API.");

        var payload = BuildPayload(request);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/messages")
        {
            Content = new StringContent(payload.ToJsonString(), Encoding.UTF8)
        };
        httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
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

        JsonNode? root;
        try
        {
            var json = await response.Content.ReadAsStringAsync(ct);
            root = JsonNode.Parse(json);
        }
        catch (JsonException)
        {
            return Error.Failure("Ai.BadResponse", "Không đọc được phản hồi từ dịch vụ AI.");
        }

        if (root is null)
            return Error.Failure("Ai.BadResponse", "Phản hồi từ dịch vụ AI rỗng.");

        var model = root["model"]?.GetValue<string>() ?? _settings.Model;
        var stopReason = root["stop_reason"]?.GetValue<string>();
        var blocks = ParseContent(root["content"] as JsonArray);

        if (blocks.Count == 0)
            return Error.Failure("Ai.BadResponse", "Phản hồi từ dịch vụ AI không có nội dung.");

        return new AssistantCompletionResult(blocks, stopReason == "tool_use", model);
    }

    private JsonObject BuildPayload(AssistantCompletionRequest request)
    {
        var payload = new JsonObject
        {
            ["model"] = _settings.Model,
            ["max_tokens"] = request.MaxTokens ?? _settings.MaxTokens
        };
        if (!string.IsNullOrWhiteSpace(request.System))
            payload["system"] = request.System;

        if (request.Tools.Count > 0)
        {
            var tools = new JsonArray();
            foreach (var t in request.Tools)
                tools.Add(BuildToolSchema(t));
            payload["tools"] = tools;
        }

        var messages = new JsonArray();
        foreach (var m in request.Messages)
            messages.Add(BuildMessage(m));
        payload["messages"] = messages;

        return payload;
    }

    private static JsonObject BuildToolSchema(AiTool tool)
    {
        var properties = new JsonObject();
        var required = new JsonArray();

        foreach (var p in tool.Parameters)
        {
            var prop = new JsonObject
            {
                ["type"] = p.Type,
                ["description"] = p.Description
            };
            if (p.Enum is { Count: > 0 })
            {
                var en = new JsonArray();
                foreach (var v in p.Enum) en.Add(v);
                prop["enum"] = en;
            }
            properties[p.Name] = prop;
            if (p.Required) required.Add(p.Name);
        }

        return new JsonObject
        {
            ["name"] = tool.Name,
            ["description"] = tool.Description,
            ["input_schema"] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = properties,
                ["required"] = required
            }
        };
    }

    private static JsonObject BuildMessage(AssistantMessage message)
    {
        var content = new JsonArray();
        foreach (var block in message.Content)
        {
            switch (block)
            {
                case AssistantText text:
                    content.Add(new JsonObject { ["type"] = "text", ["text"] = text.Text });
                    break;
                case AssistantToolUse use:
                    content.Add(new JsonObject
                    {
                        ["type"] = "tool_use",
                        ["id"] = use.Id,
                        ["name"] = use.Name,
                        ["input"] = ParseInput(use.ArgumentsJson)
                    });
                    break;
                case AssistantToolResult res:
                    content.Add(new JsonObject
                    {
                        ["type"] = "tool_result",
                        ["tool_use_id"] = res.ToolUseId,
                        ["content"] = res.Content,
                        ["is_error"] = res.IsError
                    });
                    break;
            }
        }

        return new JsonObject
        {
            ["role"] = message.Role == AssistantRole.Assistant ? "assistant" : "user",
            ["content"] = content
        };
    }

    private static JsonNode ParseInput(string argumentsJson)
    {
        if (string.IsNullOrWhiteSpace(argumentsJson))
            return new JsonObject();
        try
        {
            return JsonNode.Parse(argumentsJson) ?? new JsonObject();
        }
        catch (JsonException)
        {
            return new JsonObject();
        }
    }

    private static List<AssistantContent> ParseContent(JsonArray? content)
    {
        var blocks = new List<AssistantContent>();
        if (content is null) return blocks;

        foreach (var node in content)
        {
            if (node is not JsonObject obj) continue;
            var type = obj["type"]?.GetValue<string>();
            switch (type)
            {
                case "text":
                    var text = obj["text"]?.GetValue<string>();
                    if (!string.IsNullOrEmpty(text))
                        blocks.Add(new AssistantText(text));
                    break;
                case "tool_use":
                    var id = obj["id"]?.GetValue<string>() ?? string.Empty;
                    var name = obj["name"]?.GetValue<string>() ?? string.Empty;
                    var input = obj["input"]?.ToJsonString() ?? "{}";
                    blocks.Add(new AssistantToolUse(id, name, input));
                    break;
            }
        }

        return blocks;
    }
}
