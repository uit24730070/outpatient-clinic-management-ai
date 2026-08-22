using ClinicManagement.Domain.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagement.Infrastructure.Persistence.Configurations;

public sealed class ServicePriceConfiguration : IEntityTypeConfiguration<ServicePrice>
{
    // Thời điểm cố định cho dữ liệu seed (HasData yêu cầu giá trị tất định).
    private static readonly DateTimeOffset SeedTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<ServicePrice> builder)
    {
        builder.ToTable("service_prices");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Code)
            .HasMaxLength(20)
            .IsRequired();
        builder.HasIndex(s => s.Code).IsUnique();

        builder.Property(s => s.Name)
            .HasMaxLength(200)
            .IsRequired();
        builder.HasIndex(s => s.Name);

        // Tiền tệ: numeric(18,2), VND làm tròn về đồng (ADR 0014).
        builder.Property(s => s.UnitPrice)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(s => s.Description).HasMaxLength(1000);

        // Phân loại dịch vụ lưu dạng chuỗi (ADR 0015).
        builder.Property(s => s.Category)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Seed vài dịch vụ mẫu để lập hoá đơn được ngay sau khi áp migration.
        builder.HasData(
            SeedService("22222222-3333-4444-5555-000000000001", "DV-000001",
                "Khám tổng quát", 150000m, "Công khám bệnh thông thường", ServiceCategory.Consultation),
            SeedService("22222222-3333-4444-5555-000000000002", "DV-000002",
                "Tái khám", 100000m, "Công khám tái khám", ServiceCategory.Consultation),
            SeedService("22222222-3333-4444-5555-000000000003", "DV-000003",
                "Khám chuyên khoa", 200000m, "Công khám theo chuyên khoa", ServiceCategory.Consultation),
            // Cận lâm sàng mẫu (loại Paraclinical) để chỉ định được ngay sau khi áp migration.
            SeedService("22222222-3333-4444-5555-000000000004", "DV-000004",
                "Xét nghiệm công thức máu", 80000m, "Tổng phân tích tế bào máu ngoại vi", ServiceCategory.Paraclinical),
            SeedService("22222222-3333-4444-5555-000000000005", "DV-000005",
                "Chụp X-quang ngực thẳng", 120000m, "X-quang ngực thẳng", ServiceCategory.Paraclinical),
            SeedService("22222222-3333-4444-5555-000000000006", "DV-000006",
                "Siêu âm ổ bụng tổng quát", 150000m, "Siêu âm ổ bụng", ServiceCategory.Paraclinical));
    }

    private static object SeedService(
        string id, string code, string name, decimal unitPrice, string? description,
        ServiceCategory category) => new
    {
        Id = Guid.Parse(id),
        Code = code,
        Name = name,
        UnitPrice = unitPrice,
        Description = description,
        Category = category,
        CreatedAt = SeedTime,
        IsDeleted = false
    };
}
