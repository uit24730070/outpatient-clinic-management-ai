using ClinicManagement.Application.Common.Ai;

namespace UnitTests.Common;

/// <summary>
/// Vector store in-memory cho unit test — tính cosine trong C# (không cần pgvector).
/// Thay cho <c>PgEncounterEmbeddingStore</c> để kiểm thử luồng lập chỉ mục & truy hồi.
/// </summary>
public sealed class InMemoryEncounterEmbeddingStore : IEncounterEmbeddingStore
{
    private readonly Dictionary<Guid, EncounterEmbeddingRecord> _byEncounter = new();

    public int Count => _byEncounter.Count;

    public Task UpsertAsync(EncounterEmbeddingRecord record, CancellationToken ct = default)
    {
        _byEncounter[record.EncounterId] = record;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<EncounterMatch>> SearchAsync(
        Guid patientId, IReadOnlyList<float> query, int topK, CancellationToken ct = default)
    {
        var matches = _byEncounter.Values
            .Where(r => r.PatientId == patientId)
            .Select(r => new EncounterMatch(r.EncounterId, Cosine(r.Embedding, query)))
            .OrderByDescending(m => m.Similarity)
            .Take(topK)
            .ToList();

        return Task.FromResult<IReadOnlyList<EncounterMatch>>(matches);
    }

    private static double Cosine(IReadOnlyList<float> a, IReadOnlyList<float> b)
    {
        double dot = 0, na = 0, nb = 0;
        var n = Math.Min(a.Count, b.Count);
        for (var i = 0; i < n; i++)
        {
            dot += a[i] * (double)b[i];
            na += a[i] * (double)a[i];
            nb += b[i] * (double)b[i];
        }
        if (na == 0 || nb == 0) return 0;
        return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }
}
