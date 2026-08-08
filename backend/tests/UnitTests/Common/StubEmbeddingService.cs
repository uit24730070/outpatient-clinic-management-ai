using ClinicManagement.Application.Common.Ai;
using ClinicManagement.Shared.Results;

namespace UnitTests.Common;

/// <summary>
/// Stub <see cref="IEmbeddingService"/> — ánh xạ văn bản → vector theo hàm cấu hình sẵn, hoặc trả lỗi.
/// Không gọi mạng. Ghi lại số lần gọi và các đầu vào cuối cùng.
/// </summary>
public sealed class StubEmbeddingService : IEmbeddingService
{
    private readonly Func<string, float[]>? _map;
    private readonly Error? _error;

    public int Calls { get; private set; }
    public IReadOnlyList<string>? LastInputs { get; private set; }
    public EmbeddingInputType LastInputType { get; private set; }

    private StubEmbeddingService(Func<string, float[]>? map, Error? error)
    {
        _map = map;
        _error = error;
    }

    public static StubEmbeddingService FromMap(Func<string, float[]> map) => new(map, null);
    public static StubEmbeddingService Fail(Error error) => new(null, error);

    public Task<Result<EmbeddingResult>> EmbedAsync(EmbeddingRequest request, CancellationToken ct = default)
    {
        Calls++;
        LastInputs = request.Inputs;
        LastInputType = request.InputType;

        if (_error is { } err)
            return Task.FromResult(Result.Failure<EmbeddingResult>(err));

        var vectors = request.Inputs.Select(_map!).ToList();
        Result<EmbeddingResult> ok = new EmbeddingResult(vectors, "stub-embedding");
        return Task.FromResult(ok);
    }
}
