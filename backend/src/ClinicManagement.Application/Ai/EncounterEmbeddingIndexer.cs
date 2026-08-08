using ClinicManagement.Application.Common.Ai;
using ClinicManagement.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.Ai;

/// <summary>
/// Hiện thực sinh & lưu embedding cho phiếu khám. Nuốt mọi lỗi (mạng/embedding/store) để
/// KHÔNG chặn luồng ghi phiếu — nếu embedding thất bại, phiếu vẫn được lưu, truy hồi sẽ
/// bỏ sót phiếu đó cho tới lần lập chỉ mục kế tiếp (hoặc backfill).
/// </summary>
public sealed class EncounterEmbeddingIndexer : IEncounterEmbeddingIndexer
{
    private readonly IAppDbContext _db;
    private readonly IEmbeddingService _embeddings;
    private readonly IEncounterEmbeddingStore _store;

    public EncounterEmbeddingIndexer(
        IAppDbContext db, IEmbeddingService embeddings, IEncounterEmbeddingStore store)
    {
        _db = db;
        _embeddings = embeddings;
        _store = store;
    }

    public async Task IndexAsync(Guid encounterId, CancellationToken ct = default)
    {
        try
        {
            await IndexCoreAsync(encounterId, ct);
        }
        catch
        {
            // Best-effort: lỗi lập chỉ mục không được làm hỏng nghiệp vụ ghi phiếu.
        }
    }

    public async Task<int> ReindexAllAsync(CancellationToken ct = default)
    {
        var ids = await _db.Encounters.AsNoTracking()
            .Select(e => e.Id)
            .ToListAsync(ct);

        var done = 0;
        foreach (var id in ids)
        {
            if (await IndexCoreAsync(id, ct))
                done++;
        }
        return done;
    }

    /// <summary>Sinh + lưu embedding cho một phiếu; trả về true nếu lưu thành công.</summary>
    private async Task<bool> IndexCoreAsync(Guid encounterId, CancellationToken ct)
    {
        var content = await EncounterNarrative
            .Project(_db, _db.Encounters.AsNoTracking().Where(e => e.Id == encounterId))
            .FirstOrDefaultAsync(ct);
        if (content is null)
            return false;

        var text = EncounterNarrative.ToEmbeddingText(content);
        var result = await _embeddings.EmbedAsync(
            new EmbeddingRequest(new[] { text }, EmbeddingInputType.Document), ct);
        if (result.IsFailure || result.Value.Vectors.Count == 0)
            return false;

        await _store.UpsertAsync(
            new EncounterEmbeddingRecord(content.Id, content.PatientId, result.Value.Vectors[0], result.Value.Model),
            ct);
        return true;
    }
}
