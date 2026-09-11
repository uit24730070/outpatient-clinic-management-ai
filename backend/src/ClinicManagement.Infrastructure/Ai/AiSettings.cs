namespace ClinicManagement.Infrastructure.Ai;

/// <summary>
/// Cấu hình trợ lý AI/LLM, đọc từ section "Ai". Giá trị khoá thật đặt qua biến môi trường/secret
/// (<c>ApiKey</c> để trống ở appsettings.json, như <c>Jwt:Key</c>).
/// </summary>
public sealed class AiSettings
{
    public const string SectionName = "Ai";

    /// <summary>Khoá API của provider (OpenAI). Không commit giá trị thật (đặt qua <c>Ai__ApiKey</c>).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Model đang dùng. Mặc định GPT-5 mini (OpenAI).</summary>
    public string Model { get; set; } = "gpt-5-mini";

    /// <summary>Điểm cuối API (không kèm dấu "/" cuối). Endpoint chat: <c>{BaseUrl}/v1/chat/completions</c>.</summary>
    public string BaseUrl { get; set; } = "https://api.openai.com";

    /// <summary>
    /// Mức "suy luận" của model GPT-5 (<c>reasoning_effort</c>): none/low/medium/high/xhigh.
    /// Mặc định <c>low</c> — đủ cho tóm tắt/hỏi đáp, giảm độ trễ &amp; chi phí. Để trống ⇒ không gửi tham số.
    /// </summary>
    public string ReasoningEffort { get; set; } = "low";

    /// <summary>
    /// Giới hạn token đầu ra mỗi lần gọi (<c>max_completion_tokens</c>). Với model GPT-5, token này
    /// <b>bao gồm cả token suy luận</b> nên đặt dư (mặc định 2048) để tránh cụt câu trả lời.
    /// </summary>
    public int MaxTokens { get; set; } = 2048;

    /// <summary>Thời gian chờ tối đa (giây) cho một lần gọi LLM (model suy luận có thể chậm hơn).</summary>
    public int TimeoutSeconds { get; set; } = 120;

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
