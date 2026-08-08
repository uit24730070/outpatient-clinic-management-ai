using ClinicManagement.Shared.Results;

namespace ClinicManagement.Application.Common.Ai;

/// <summary>
/// Trừu tượng sinh embedding — Application/Domain chỉ phụ thuộc interface này, không biết provider
/// (Voyage AI/…). Hiện thực (HTTP client thật hoặc fake) nằm ở Infrastructure.
/// Lỗi gói vào <see cref="Error"/> mã <c>Embedding.*</c> thay vì ném exception.
/// </summary>
public interface IEmbeddingService
{
    Task<Result<EmbeddingResult>> EmbedAsync(EmbeddingRequest request, CancellationToken ct = default);
}
