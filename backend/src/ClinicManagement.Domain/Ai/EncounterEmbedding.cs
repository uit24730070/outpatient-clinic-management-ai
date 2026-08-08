using ClinicManagement.Domain.Common;

namespace ClinicManagement.Domain.Ai;

/// <summary>
/// Embedding (vector ngữ nghĩa) của một phiếu khám, phục vụ truy hồi RAG.
/// Quan hệ 1–1 với phiếu khám (unique <see cref="EncounterId"/>); <see cref="PatientId"/> snapshot
/// để lọc truy hồi theo bệnh nhân mà không cần join.
/// </summary>
public sealed class EncounterEmbedding : Entity
{
    // Ctor cho EF.
    private EncounterEmbedding()
    {
        Embedding = Array.Empty<float>();
        Model = string.Empty;
    }

    public EncounterEmbedding(Guid encounterId, Guid patientId, float[] embedding, string model)
    {
        EncounterId = encounterId;
        PatientId = patientId;
        Embedding = embedding;
        Model = model;
    }

    public Guid EncounterId { get; private set; }
    public Guid PatientId { get; private set; }

    /// <summary>Vector embedding. Ánh xạ cột <c>vector</c> của pgvector ở Infrastructure.</summary>
    public float[] Embedding { get; private set; }

    /// <summary>Model đã sinh embedding (để đối chiếu khi đổi provider/model → cần re-embed).</summary>
    public string Model { get; private set; }

    public void Update(float[] embedding, string model)
    {
        Embedding = embedding;
        Model = model;
    }
}
