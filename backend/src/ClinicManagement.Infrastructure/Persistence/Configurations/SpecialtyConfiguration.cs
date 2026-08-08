using ClinicManagement.Domain.Specialties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagement.Infrastructure.Persistence.Configurations;

public sealed class SpecialtyConfiguration : IEntityTypeConfiguration<Specialty>
{
    // Thời điểm cố định cho dữ liệu seed (HasData yêu cầu giá trị tất định để migration ổn định).
    private static readonly DateTimeOffset SeedTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<Specialty> builder)
    {
        builder.ToTable("specialties");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .HasMaxLength(150)
            .IsRequired();
        builder.HasIndex(s => s.Name).IsUnique();

        builder.Property(s => s.Description).HasMaxLength(500);

        // Seed vài chuyên khoa mẫu để có sẵn khoá ngoại cho Bác sĩ.
        builder.HasData(
            Seed("11111111-1111-1111-1111-111111111111", "Nội tổng quát", "Khám và điều trị bệnh nội khoa chung."),
            Seed("22222222-2222-2222-2222-222222222222", "Tim mạch", "Chẩn đoán và điều trị bệnh lý tim mạch."),
            Seed("33333333-3333-3333-3333-333333333333", "Nhi khoa", "Khám và điều trị cho trẻ em."),
            Seed("44444444-4444-4444-4444-444444444444", "Tai mũi họng", null),
            Seed("55555555-5555-5555-5555-555555555555", "Da liễu", null));
    }

    private static object Seed(string id, string name, string? description) => new
    {
        Id = Guid.Parse(id),
        Name = name,
        Description = description,
        CreatedAt = SeedTime,
        IsDeleted = false
    };
}
