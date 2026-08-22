using ClinicManagement.Domain.Pharmacy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagement.Infrastructure.Persistence.Configurations;

public sealed class StockReceiptConfiguration : IEntityTypeConfiguration<StockReceipt>
{
    public void Configure(EntityTypeBuilder<StockReceipt> builder)
    {
        builder.ToTable("stock_receipts");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Code)
            .HasMaxLength(20)
            .IsRequired();
        builder.HasIndex(r => r.Code).IsUnique();

        builder.Property(r => r.SupplierName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.ReceivedAt).IsRequired();
        builder.Property(r => r.Note).HasMaxLength(1000);

        // Cha–con: owned collection ở bảng riêng, vòng đời gắn chặt phiếu nhập (như PrescriptionItem, ADR 0006).
        builder.OwnsMany(r => r.Items, item =>
        {
            item.ToTable("stock_receipt_items");
            item.WithOwner().HasForeignKey("StockReceiptId");
            item.Property<int>("Id");
            item.HasKey("Id");

            item.Property(i => i.BatchNumber).HasMaxLength(100).IsRequired();
            item.Property(i => i.ExpiryDate).HasColumnType("date").IsRequired();
            item.Property(i => i.Quantity).IsRequired();
            item.Property(i => i.UnitCost).HasColumnType("numeric(18,2)");

            item.HasIndex(i => i.MedicationId);
        });

        // Đọc/ghi cụm dòng nhập qua backing field (property chỉ đọc).
        builder.Navigation(r => r.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
