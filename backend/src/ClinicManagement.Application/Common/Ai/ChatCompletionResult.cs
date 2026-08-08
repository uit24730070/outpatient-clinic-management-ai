namespace ClinicManagement.Application.Common.Ai;

/// <summary>Kết quả sinh văn bản từ LLM: nội dung trả về và model đã dùng.</summary>
public sealed record ChatCompletionResult(string Text, string Model);
