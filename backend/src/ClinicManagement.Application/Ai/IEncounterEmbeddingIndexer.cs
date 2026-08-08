namespace ClinicManagement.Application.Ai;

/// <summary>
/// Sinh và lưu embedding cho phiếu khám vào vector store (AI-04). Chạy "best-effort" — lỗi
/// sinh/lưu embedding KHÔNG được chặn nghiệp vụ ghi phiếu (ADR RAG).
/// </summary>
public interface IEncounterEmbeddingIndexer
{
    /// <summary>Lập chỉ mục lại embedding cho một phiếu khám (gọi sau khi tạo/sửa/chốt phiếu).</summary>
    Task IndexAsync(Guid encounterId, CancellationToken ct = default);

    /// <summary>Backfill: lập chỉ mục embedding cho toàn bộ phiếu khám hiện có. Trả về số phiếu đã xử lý.</summary>
    Task<int> ReindexAllAsync(CancellationToken ct = default);
}
