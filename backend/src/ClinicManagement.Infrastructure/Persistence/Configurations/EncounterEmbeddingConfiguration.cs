using ClinicManagement.Domain.Ai;
using ClinicManagement.Domain.Encounters;
using ClinicManagement.Infrastructure.Ai;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pgvector;

namespace ClinicManagement.Infrastructure.Persistence.Configurations;

public sealed class EncounterEmbeddingConfiguration : IEntityTypeConfiguration<EncounterEmbedding>
{
    // So sánh vector theo phần tử (float[] vốn so sánh theo tham chiếu) để EF theo dõi thay đổi đúng.
    private static readonly ValueComparer<float[]> VectorComparer = new(
        (a, b) => a != null && b != null && a.SequenceEqual(b),
        v => v.Aggregate(0, (h, f) => HashCode.Combine(h, f.GetHashCode())),
        v => v.ToArray());

    public void Configure(EntityTypeBuilder<EncounterEmbedding> builder)
    {
        builder.ToTable("encounter_embeddings");

        builder.HasKey(e => e.Id);

        // Cột vector của pgvector: map float[] (Domain, EF-agnostic) ↔ Pgvector.Vector (chỉ ở Infrastructure).
        builder.Property(e => e.Embedding)
            .HasColumnType($"vector({AiSettings.EmbeddingDimensions})")
            .HasConversion(
                v => new Vector(v),
                v => v.ToArray(),
                VectorComparer)
            .IsRequired();

        builder.Property(e => e.Model).HasMaxLength(100).IsRequired();

        // 1–1 với phiếu khám + truy hồi theo bệnh nhân.
        builder.HasIndex(e => e.EncounterId).IsUnique();
        builder.HasIndex(e => e.PatientId);

        builder.HasOne<Encounter>()
            .WithMany()
            .HasForeignKey(e => e.EncounterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
