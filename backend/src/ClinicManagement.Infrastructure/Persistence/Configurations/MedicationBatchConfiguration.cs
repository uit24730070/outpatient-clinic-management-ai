using ClinicManagement.Domain.Pharmacy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagement.Infrastructure.Persistence.Configurations;

public sealed class MedicationBatchConfiguration : IEntityTypeConfiguration<MedicationBatch>
{
    public void Configure(EntityTypeBuilder<MedicationBatch> builder)
    {
        builder.ToTable("medication_batches");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.BatchNumber)
            .HasMaxLength(100)
            .IsRequired();

        // DateOnly ↔ Postgres date (Npgsql hỗ trợ trực tiếp).
        builder.Property(b => b.ExpiryDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(b => b.QuantityOnHand).IsRequired();

        // Truy hồi lô theo thuốc, hạn tăng dần (định hướng FEFO ở P2).
        builder.HasIndex(b => new { b.MedicationId, b.ExpiryDate });

        // Khoá ngoại tới Thuốc; chặn xoá (vật lý) khi còn lô tham chiếu.
        builder.HasOne<Medication>()
            .WithMany()
            .HasForeignKey(b => b.MedicationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
