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

        // Seed vài dịch vụ mẫu để lập hoá đơn được ngay sau khi áp migration.
        builder.HasData(
            SeedService("22222222-3333-4444-5555-000000000001", "DV-000001",
                "Khám tổng quát", 150000m, "Công khám bệnh thông thường"),
            SeedService("22222222-3333-4444-5555-000000000002", "DV-000002",
                "Tái khám", 100000m, "Công khám tái khám"),
            SeedService("22222222-3333-4444-5555-000000000003", "DV-000003",
                "Khám chuyên khoa", 200000m, "Công khám theo chuyên khoa"));
    }

    private static object SeedService(
        string id, string code, string name, decimal unitPrice, string? description) => new
    {
        Id = Guid.Parse(id),
        Code = code,
        Name = name,
        UnitPrice = unitPrice,
        Description = description,
        CreatedAt = SeedTime,
        IsDeleted = false
    };
}
