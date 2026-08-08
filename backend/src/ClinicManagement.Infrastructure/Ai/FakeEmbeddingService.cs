using System.Security.Cryptography;
using ClinicManagement.Application.Common.Ai;
using ClinicManagement.Shared.Results;

namespace ClinicManagement.Infrastructure.Ai;

/// <summary>
/// Hiện thực fake của <see cref="IEmbeddingService"/> — sinh vector <b>tất định</b> từ nội dung
/// văn bản, không gọi mạng. Dùng khi thiếu khoá hoặc bật <c>Ai:UseFakeEmbedding</c> (dev/test).
/// Vector được chuẩn hoá đơn vị; văn bản giống nhau → vector giống nhau (đủ để minh hoạ RAG).
/// </summary>
public sealed class FakeEmbeddingService : IEmbeddingService
{
    public const string ModelName = "fake-embedding";

    private readonly int _dimensions;

    public FakeEmbeddingService(int dimensions = AiSettings.EmbeddingDimensions) => _dimensions = dimensions;

    public Task<Result<EmbeddingResult>> EmbedAsync(
        EmbeddingRequest request, CancellationToken ct = default)
    {
        var vectors = request.Inputs.Select(Embed).ToList();
        Result<EmbeddingResult> result = new EmbeddingResult(vectors, ModelName);
        return Task.FromResult(result);
    }

    /// <summary>Sinh vector tất định: gieo mầm từ SHA-256 của văn bản, rồi chuẩn hoá đơn vị.</summary>
    private float[] Embed(string text)
    {
        var seed = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text ?? string.Empty));
        var rng = new Random(BitConverter.ToInt32(seed, 0));

        var v = new float[_dimensions];
        double norm = 0;
        for (var i = 0; i < _dimensions; i++)
        {
            v[i] = (float)(rng.NextDouble() * 2 - 1);
            norm += v[i] * (double)v[i];
        }

        norm = Math.Sqrt(norm);
        if (norm > 0)
            for (var i = 0; i < _dimensions; i++)
                v[i] = (float)(v[i] / norm);

        return v;
    }
}
