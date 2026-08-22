using ClinicManagement.Domain.Pharmacy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagement.Infrastructure.Persistence.Configurations;

public sealed class StockTransactionConfiguration : IEntityTypeConfiguration<StockTransaction>
{
    public void Configure(EntityTypeBuilder<StockTransaction> builder)
    {
        builder.ToTable("stock_transactions");

        builder.HasKey(t => t.Id);

        // Enum lưu chuỗi (đồng nhất Gender/UserRole/AppointmentStatus/EncounterStatus).
        builder.Property(t => t.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.QuantityDelta).IsRequired();

        builder.Property(t => t.ReferenceType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.OccurredAt).IsRequired();

        // Sổ cái theo lô (thứ tự thời gian) + tra theo chứng từ nguồn.
        builder.HasIndex(t => new { t.MedicationBatchId, t.OccurredAt });
        builder.HasIndex(t => new { t.ReferenceType, t.ReferenceId });

        // Khoá ngoại tới lô; chặn xoá lô khi còn giao dịch tham chiếu.
        builder.HasOne<MedicationBatch>()
            .WithMany()
            .HasForeignKey(t => t.MedicationBatchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
