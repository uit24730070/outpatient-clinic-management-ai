namespace ClinicManagement.Application.Common.Ai;

/// <summary>
/// Yêu cầu sinh embedding cho một hoặc nhiều đoạn văn bản (trung lập provider).
/// Model/số chiều do hiện thực ở Infrastructure điền theo cấu hình.
/// </summary>
public sealed record EmbeddingRequest(
    IReadOnlyList<string> Inputs,
    EmbeddingInputType InputType = EmbeddingInputType.Document);
