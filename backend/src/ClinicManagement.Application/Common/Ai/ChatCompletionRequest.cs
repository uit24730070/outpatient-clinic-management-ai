namespace ClinicManagement.Application.Common.Ai;

/// <summary>
/// Yêu cầu sinh văn bản trung lập provider gửi tới <see cref="IChatCompletionService"/>.
/// Các tuỳ chọn bỏ trống (null) sẽ dùng mặc định do hiện thực ở Infrastructure cấu hình (model, maxTokens).
/// </summary>
public sealed record ChatCompletionRequest(
    IReadOnlyList<ChatMessage> Messages,
    string? System = null,
    int? MaxTokens = null,
    double? Temperature = null);
