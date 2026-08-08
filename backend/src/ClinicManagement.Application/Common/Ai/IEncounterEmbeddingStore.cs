namespace ClinicManagement.Application.Common.Ai;

/// <summary>Một bản ghi embedding của phiếu khám cần lưu/cập nhật vào vector store.</summary>
public sealed record EncounterEmbeddingRecord(
    Guid EncounterId,
    Guid PatientId,
    float[] Embedding,
    string Model);

/// <summary>Kết quả truy hồi: phiếu khám gần nhất với truy vấn và độ tương đồng cosine (0..1).</summary>
public sealed record EncounterMatch(Guid EncounterId, double Similarity);

/// <summary>
/// Trừu tượng vector store cho embedding phiếu khám. Hiện thực thật (pgvector trên PostgreSQL)
/// nằm ở Infrastructure; unit test dùng bản in-memory tính cosine trong bộ nhớ.
/// </summary>
public interface IEncounterEmbeddingStore
{
    /// <summary>Lưu mới hoặc cập nhật embedding cho một phiếu khám (theo <c>EncounterId</c>).</summary>
    Task UpsertAsync(EncounterEmbeddingRecord record, CancellationToken ct = default);

    /// <summary>Truy hồi top-K phiếu khám của bệnh nhân gần nhất với vector truy vấn (cosine, giảm dần).</summary>
    Task<IReadOnlyList<EncounterMatch>> SearchAsync(
        Guid patientId, IReadOnlyList<float> query, int topK, CancellationToken ct = default);
}
