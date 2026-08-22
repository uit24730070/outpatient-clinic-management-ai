using ClinicManagement.Domain.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagement.Infrastructure.Persistence.Configurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Code)
            .HasMaxLength(20)
            .IsRequired();
        builder.HasIndex(i => i.Code).IsUnique();

        builder.HasIndex(i => i.PatientId);

        // 1–1 với phiếu khám khi lập từ phiếu; nullable → nhiều NULL (hoá đơn lẻ) hợp lệ trên Postgres.
        builder.HasIndex(i => i.EncounterId).IsUnique();

        // Enum trạng thái/phương thức thu lưu dạng chuỗi (đồng nhất Gender/UserRole/AppointmentStatus).
        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(i => i.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Tiền tệ: numeric(18,2), VND làm tròn về đồng (ADR 0014).
        builder.Property(i => i.TotalAmount)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(i => i.Note).HasMaxLength(1000);

        // Cha–con: owned collection ở bảng riêng, vòng đời gắn chặt hoá đơn (như PrescriptionItem, ADR 0006).
        builder.OwnsMany(i => i.Items, item =>
        {
            item.ToTable("invoice_items");
            item.WithOwner().HasForeignKey("InvoiceId");
            item.Property<int>("Id");
            item.HasKey("Id");

            item.Property(x => x.ItemType)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            item.Property(x => x.Description).HasMaxLength(300).IsRequired();
            item.Property(x => x.UnitPrice).HasColumnType("numeric(18,2)").IsRequired();
            item.Property(x => x.Quantity).IsRequired();
            item.Property(x => x.LineTotal).HasColumnType("numeric(18,2)").IsRequired();
        });

        // Đọc/ghi cụm dòng hoá đơn qua backing field (property chỉ đọc).
        builder.Navigation(i => i.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
