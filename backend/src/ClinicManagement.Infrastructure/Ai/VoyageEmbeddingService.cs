using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClinicManagement.Application.Common.Ai;
using ClinicManagement.Shared.Results;
using Microsoft.Extensions.Options;

namespace ClinicManagement.Infrastructure.Ai;

/// <summary>
/// Hiện thực <see cref="IEmbeddingService"/> gọi Voyage AI qua HTTP thuần (<c>POST /v1/embeddings</c>).
/// Mọi lỗi mạng/parse được gói vào <see cref="Error"/> mã <c>Embedding.*</c>.
/// </summary>
public sealed class VoyageEmbeddingService : IEmbeddingService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly AiSettings _settings;

    public VoyageEmbeddingService(HttpClient http, IOptions<AiSettings> settings)
    {
        _http = http;
        _settings = settings.Value;
    }

    public async Task<Result<EmbeddingResult>> EmbedAsync(
        EmbeddingRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.EmbeddingApiKey))
            return Error.Failure("Embedding.Unavailable", "Dịch vụ embedding chưa được cấu hình khoá API.");

        if (request.Inputs.Count == 0)
            return Error.Failure("Embedding.BadResponse", "Không có đầu vào để sinh embedding.");

        var payload = new VoyageRequest
        {
            Model = _settings.EmbeddingModel,
            Input = request.Inputs.ToList(),
            InputType = request.InputType == EmbeddingInputType.Query ? "query" : "document"
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/embeddings")
        {
            Content = JsonContent.Create(payload, options: JsonOptions)
        };
        httpRequest.Headers.Add("Authorization", $"Bearer {_settings.EmbeddingApiKey}");

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(httpRequest, ct);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return Error.Failure("Embedding.Timeout", "Dịch vụ embedding phản hồi quá lâu, vui lòng thử lại.");
        }
        catch (HttpRequestException ex)
        {
            return Error.Failure("Embedding.Unavailable", $"Không kết nối được tới dịch vụ embedding: {ex.Message}");
        }

        if (!response.IsSuccessStatusCode)
            return Error.Failure("Embedding.Unavailable",
                $"Dịch vụ embedding trả về mã lỗi {(int)response.StatusCode}.");

        VoyageResponse? parsed;
        try
        {
            parsed = await response.Content.ReadFromJsonAsync<VoyageResponse>(JsonOptions, ct);
        }
        catch (JsonException)
        {
            return Error.Failure("Embedding.BadResponse", "Không đọc được phản hồi từ dịch vụ embedding.");
        }

        var vectors = parsed?.Data?
            .OrderBy(d => d.Index)
            .Select(d => d.Embedding)
            .Where(v => v is { Length: > 0 })
            .Select(v => v!)
            .ToList();

        if (vectors is null || vectors.Count == 0)
            return Error.Failure("Embedding.BadResponse", "Phản hồi embedding rỗng hoặc không hợp lệ.");

        return new EmbeddingResult(vectors, parsed!.Model ?? _settings.EmbeddingModel);
    }

    // --- DTO khớp wire-format Voyage AI Embeddings API (chỉ ở Infrastructure) ---

    private sealed class VoyageRequest
    {
        [JsonPropertyName("model")] public string Model { get; init; } = string.Empty;
        [JsonPropertyName("input")] public List<string> Input { get; init; } = new();
        [JsonPropertyName("input_type")] public string? InputType { get; init; }
    }

    private sealed class VoyageResponse
    {
        [JsonPropertyName("model")] public string? Model { get; init; }
        [JsonPropertyName("data")] public List<VoyageEmbedding>? Data { get; init; }
    }

    private sealed class VoyageEmbedding
    {
        [JsonPropertyName("index")] public int Index { get; init; }
        [JsonPropertyName("embedding")] public float[]? Embedding { get; init; }
    }
}
