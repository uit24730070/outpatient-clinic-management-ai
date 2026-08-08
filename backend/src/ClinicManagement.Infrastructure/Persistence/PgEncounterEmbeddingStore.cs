using System.Globalization;
using ClinicManagement.Application.Common.Ai;
using ClinicManagement.Domain.Ai;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Infrastructure.Persistence;

/// <summary>
/// Vector store trên PostgreSQL + pgvector. Upsert qua EF; truy hồi top-K bằng SQL thô dùng toán tử
/// khoảng cách cosine <c>&lt;=&gt;</c> (độ tương đồng = 1 − khoảng cách), lọc theo bệnh nhân và loại
/// bản ghi đã xoá mềm. SQL/pgvector chỉ tồn tại ở Infrastructure.
/// </summary>
public sealed class PgEncounterEmbeddingStore : IEncounterEmbeddingStore
{
    private readonly AppDbContext _db;

    public PgEncounterEmbeddingStore(AppDbContext db) => _db = db;

    public async Task UpsertAsync(EncounterEmbeddingRecord record, CancellationToken ct = default)
    {
        var existing = await _db.EncounterEmbeddings
            .FirstOrDefaultAsync(e => e.EncounterId == record.EncounterId, ct);

        if (existing is null)
        {
            _db.EncounterEmbeddings.Add(
                new EncounterEmbedding(record.EncounterId, record.PatientId, record.Embedding, record.Model));
        }
        else
        {
            existing.Update(record.Embedding, record.Model);
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<EncounterMatch>> SearchAsync(
        Guid patientId, IReadOnlyList<float> query, int topK, CancellationToken ct = default)
    {
        // Literal pgvector dạng "[f1,f2,...]" ép sang kiểu vector bằng CAST.
        var literal = "[" + string.Join(",", query.Select(f => f.ToString(CultureInfo.InvariantCulture))) + "]";

        // Tên cột giữ PascalCase (EF/Npgsql không snake_case hoá) → phải quote đúng hoa/thường,
        // nếu không Postgres sẽ fold về lowercase và báo "column does not exist".
        var rows = await _db.Database.SqlQuery<EncounterMatch>($@"
            SELECT ""EncounterId"" AS ""EncounterId"",
                   (1 - (""Embedding"" <=> CAST({literal} AS vector))) AS ""Similarity""
            FROM encounter_embeddings
            WHERE ""PatientId"" = {patientId} AND ""IsDeleted"" = false
            ORDER BY ""Embedding"" <=> CAST({literal} AS vector)
            LIMIT {topK}").ToListAsync(ct);

        return rows;
    }
}
