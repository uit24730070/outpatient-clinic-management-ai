using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ClinicManagement.Application.Common.Ai;
using ClinicManagement.Shared.Results;
using Microsoft.Extensions.Options;

namespace ClinicManagement.Infrastructure.Ai;

/// <summary>
/// Hiện thực <see cref="IAssistantCompletionService"/> gọi OpenAI Chat Completions API với
/// <b>tool calling</b> qua HTTP thuần (<c>POST /v1/chat/completions</c>, model GPT-5 mini).
/// Dịch DTO công cụ trung lập sang định dạng <c>tools[].function</c> của OpenAI và phân giải các
/// khối <c>tool_calls</c>/<c>content</c> trong phản hồi. Mọi lỗi mạng/parse gói vào
/// <see cref="Error"/> mã <c>Ai.*</c>.
/// </summary>
public sealed class OpenAiAssistantCompletionService : IAssistantCompletionService
{
    private readonly HttpClient _http;
    private readonly AiSettings _settings;

    public OpenAiAssistantCompletionService(HttpClient http, IOptions<AiSettings> settings)
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

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
        {
            Content = new StringContent(payload.ToJsonString(), Encoding.UTF8)
        };
        httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
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
        var choice = (root["choices"] as JsonArray)?.FirstOrDefault() as JsonObject;
        var message = choice?["message"] as JsonObject;
        var finishReason = choice?["finish_reason"]?.GetValue<string>();
        var blocks = ParseMessage(message);

        if (blocks.Count == 0)
            return Error.Failure("Ai.BadResponse", "Phản hồi từ dịch vụ AI không có nội dung.");

        var isToolUse = finishReason == "tool_calls"
            || blocks.Any(b => b is AssistantToolUse);
        return new AssistantCompletionResult(blocks, isToolUse, model);
    }

    private JsonObject BuildPayload(AssistantCompletionRequest request)
    {
        var payload = new JsonObject
        {
            ["model"] = _settings.Model,
            ["max_completion_tokens"] = request.MaxTokens ?? _settings.MaxTokens
        };
        if (!string.IsNullOrWhiteSpace(_settings.ReasoningEffort))
            payload["reasoning_effort"] = _settings.ReasoningEffort;

        if (request.Tools.Count > 0)
        {
            var tools = new JsonArray();
            foreach (var t in request.Tools)
                tools.Add(BuildToolSchema(t));
            payload["tools"] = tools;
        }

        var messages = new JsonArray();
        if (!string.IsNullOrWhiteSpace(request.System))
            messages.Add(new JsonObject { ["role"] = "system", ["content"] = request.System });
        foreach (var m in request.Messages)
            AppendMessages(messages, m);
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
            ["type"] = "function",
            ["function"] = new JsonObject
            {
                ["name"] = tool.Name,
                ["description"] = tool.Description,
                ["parameters"] = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = properties,
                    ["required"] = required
                }
            }
        };
    }

    /// <summary>
    /// Dịch một lượt hội thoại trung lập sang (một hoặc nhiều) message theo định dạng OpenAI:
    /// lượt trợ lý gộp text + <c>tool_calls</c>; lượt người dùng tách mỗi kết quả công cụ thành một
    /// message <c>role=tool</c> riêng (đúng ràng buộc ghép cặp tool_call_id của OpenAI).
    /// </summary>
    private static void AppendMessages(JsonArray messages, AssistantMessage message)
    {
        if (message.Role == AssistantRole.Assistant)
        {
            var texts = message.Content.OfType<AssistantText>().Select(t => t.Text);
            var joined = string.Join("\n", texts);
            var toolUses = message.Content.OfType<AssistantToolUse>().ToList();

            var obj = new JsonObject { ["role"] = "assistant" };
            obj["content"] = string.IsNullOrEmpty(joined) ? null : joined;
            if (toolUses.Count > 0)
            {
                var calls = new JsonArray();
                foreach (var use in toolUses)
                {
                    calls.Add(new JsonObject
                    {
                        ["id"] = use.Id,
                        ["type"] = "function",
                        ["function"] = new JsonObject
                        {
                            ["name"] = use.Name,
                            ["arguments"] = string.IsNullOrWhiteSpace(use.ArgumentsJson)
                                ? "{}" : use.ArgumentsJson
                        }
                    });
                }
                obj["tool_calls"] = calls;
            }
            messages.Add(obj);
            return;
        }

        // Lượt người dùng: văn bản thường và/hoặc các khối kết quả công cụ.
        foreach (var block in message.Content)
        {
            switch (block)
            {
                case AssistantText text:
                    messages.Add(new JsonObject { ["role"] = "user", ["content"] = text.Text });
                    break;
                case AssistantToolResult res:
                    messages.Add(new JsonObject
                    {
                        ["role"] = "tool",
                        ["tool_call_id"] = res.ToolUseId,
                        ["content"] = res.Content
                    });
                    break;
            }
        }
    }

    private static List<AssistantContent> ParseMessage(JsonObject? message)
    {
        var blocks = new List<AssistantContent>();
        if (message is null) return blocks;

        var content = message["content"]?.GetValue<string>();
        if (!string.IsNullOrEmpty(content))
            blocks.Add(new AssistantText(content));

        if (message["tool_calls"] is JsonArray toolCalls)
        {
            foreach (var node in toolCalls)
            {
                if (node is not JsonObject call) continue;
                var fn = call["function"] as JsonObject;
                var id = call["id"]?.GetValue<string>() ?? string.Empty;
                var name = fn?["name"]?.GetValue<string>() ?? string.Empty;
                var args = fn?["arguments"]?.GetValue<string>() ?? "{}";
                blocks.Add(new AssistantToolUse(id, name, args));
            }
        }

        return blocks;
    }
}
