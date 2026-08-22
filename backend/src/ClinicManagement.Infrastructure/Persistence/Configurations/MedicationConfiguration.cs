using ClinicManagement.Domain.Pharmacy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagement.Infrastructure.Persistence.Configurations;

public sealed class MedicationConfiguration : IEntityTypeConfiguration<Medication>
{
    // Thời điểm cố định cho dữ liệu seed (HasData yêu cầu giá trị tất định).
    private static readonly DateTimeOffset SeedTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<Medication> builder)
    {
        builder.ToTable("medications");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Code)
            .HasMaxLength(20)
            .IsRequired();
        builder.HasIndex(m => m.Code).IsUnique();

        builder.Property(m => m.Name)
            .HasMaxLength(200)
            .IsRequired();
        builder.HasIndex(m => m.Name);

        builder.Property(m => m.ActiveIngredient)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(m => m.Unit)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.ReorderLevel).IsRequired();
        builder.Property(m => m.Description).HasMaxLength(1000);

        // Seed vài thuốc mẫu để demo danh mục ngay sau khi áp migration.
        builder.HasData(
            SeedMedication("11111111-2222-3333-4444-000000000001", "TH-000001",
                "Paracetamol 500mg", "Paracetamol", "viên", 100),
            SeedMedication("11111111-2222-3333-4444-000000000002", "TH-000002",
                "Amoxicillin 500mg", "Amoxicillin", "viên", 50),
            SeedMedication("11111111-2222-3333-4444-000000000003", "TH-000003",
                "Oresol", "Oral rehydration salts", "gói", 30));
    }

    private static object SeedMedication(
        string id, string code, string name, string activeIngredient, string unit, int reorderLevel) => new
    {
        Id = Guid.Parse(id),
        Code = code,
        Name = name,
        ActiveIngredient = activeIngredient,
        Unit = unit,
        ReorderLevel = reorderLevel,
        Description = (string?)null,
        CreatedAt = SeedTime,
        IsDeleted = false
    };
}
