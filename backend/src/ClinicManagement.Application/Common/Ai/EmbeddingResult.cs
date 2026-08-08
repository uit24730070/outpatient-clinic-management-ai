namespace ClinicManagement.Application.Common.Ai;

/// <summary>
/// Kết quả sinh embedding: danh sách vector (mỗi phần tử ứng với một đầu vào theo thứ tự)
/// và model đã dùng.
/// </summary>
public sealed record EmbeddingResult(IReadOnlyList<float[]> Vectors, string Model);
