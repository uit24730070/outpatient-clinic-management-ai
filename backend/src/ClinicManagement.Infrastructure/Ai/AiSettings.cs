namespace ClinicManagement.Infrastructure.Ai;

/// <summary>
/// Cấu hình trợ lý AI/LLM, đọc từ section "Ai". Giá trị khoá thật đặt qua biến môi trường/secret
/// (<c>ApiKey</c> để trống ở appsettings.json, như <c>Jwt:Key</c>).
/// </summary>
public sealed class AiSettings
{
    public const string SectionName = "Ai";

    /// <summary>Khoá API của provider (Claude/Anthropic). Không commit giá trị thật.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Model đang dùng. Mặc định model Claude mới nhất.</summary>
    public string Model { get; set; } = "claude-opus-4-8";

    /// <summary>Điểm cuối API (không kèm dấu "/" cuối). Endpoint chat: <c>{BaseUrl}/v1/messages</c>.</summary>
    public string BaseUrl { get; set; } = "https://api.anthropic.com";

    /// <summary>Phiên bản API Anthropic (header <c>anthropic-version</c>).</summary>
    public string AnthropicVersion { get; set; } = "2023-06-01";

    /// <summary>Giới hạn token đầu ra mỗi lần gọi.</summary>
    public int MaxTokens { get; set; } = 1024;

    /// <summary>Thời gian chờ tối đa (giây) cho một lần gọi LLM.</summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>Bật chế độ giả lập (fake) — trả tóm tắt tất định, không gọi mạng (dev/test/thiếu khoá).</summary>
    public bool UseFake { get; set; }

    /// <summary>Đã cấu hình đủ để gọi thật? (không dùng fake và có khoá).</summary>
    public bool IsRealClientConfigured => !UseFake && !string.IsNullOrWhiteSpace(ApiKey);

    // --- Embedding (RAG) — provider mặc định Voyage AI, khoá riêng với LLM ---

    /// <summary>Số chiều vector embedding. Phải khớp cột <c>vector(N)</c> ở CSDL (mặc định voyage-3 = 1024).</summary>
    public const int EmbeddingDimensions = 1024;

    /// <summary>Khoá API của provider embedding (Voyage AI). Không commit giá trị thật (đặt qua <c>Ai__EmbeddingApiKey</c>).</summary>
    public string EmbeddingApiKey { get; set; } = string.Empty;

    /// <summary>Model embedding đang dùng.</summary>
    public string EmbeddingModel { get; set; } = "voyage-3";

    /// <summary>Điểm cuối API embedding (không kèm "/" cuối). Endpoint: <c>{BaseUrl}/v1/embeddings</c>.</summary>
    public string EmbeddingBaseUrl { get; set; } = "https://api.voyageai.com";

    /// <summary>Bật chế độ fake embedding — sinh vector tất định, không gọi mạng.</summary>
    public bool UseFakeEmbedding { get; set; }

    /// <summary>Đã cấu hình đủ để gọi provider embedding thật? (không dùng fake và có khoá).</summary>
    public bool IsRealEmbeddingConfigured => !UseFakeEmbedding && !string.IsNullOrWhiteSpace(EmbeddingApiKey);
}
