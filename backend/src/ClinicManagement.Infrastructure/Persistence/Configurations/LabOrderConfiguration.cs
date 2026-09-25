using ClinicManagement.Domain.Appointments;
using ClinicManagement.Domain.Doctors;
using ClinicManagement.Domain.Encounters;
using ClinicManagement.Domain.Paraclinical;
using ClinicManagement.Domain.Patients;
using ClinicManagement.Domain.Visits;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagement.Infrastructure.Persistence.Configurations;

public sealed class LabOrderConfiguration : IEntityTypeConfiguration<LabOrder>
{
    public void Configure(EntityTypeBuilder<LabOrder> builder)
    {
        builder.ToTable("lab_orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Code)
            .HasMaxLength(20)
            .IsRequired();
        builder.HasIndex(o => o.Code).IsUnique();

        // Tra phiếu chỉ định theo phiếu khám / bệnh nhân / lượt tiếp nhận (walk-in).
        builder.HasIndex(o => o.EncounterId);
        builder.HasIndex(o => o.PatientId);
        builder.HasIndex(o => o.AppointmentId);
        builder.HasIndex(o => o.VisitId);

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(o => o.Note).HasMaxLength(1000);

        // Tổng phí là thuộc tính tính toán từ cụm mục — không map cột.
        builder.Ignore(o => o.TotalAmount);

        // Khoá ngoại tới phiếu khám/bệnh nhân/bác sĩ; chặn xoá (vật lý) khi còn phiếu chỉ định tham chiếu.
        // Encounter/Doctor nay tuỳ chọn (null với walk-in — ADR 0016); Appointment gắn tuỳ chọn cho walk-in.
        builder.HasOne<Encounter>()
            .WithMany()
            .HasForeignKey(o => o.EncounterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(o => o.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Doctor>()
            .WithMany()
            .HasForeignKey(o => o.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Appointment>()
            .WithMany()
            .HasForeignKey(o => o.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Visit>()
            .WithMany()
            .HasForeignKey(o => o.VisitId)
            .OnDelete(DeleteBehavior.Restrict);

        // Cha–con: owned collection ở bảng riêng, vòng đời gắn chặt phiếu chỉ định (như InvoiceItem, ADR 0015).
        builder.OwnsMany(o => o.Items, item =>
        {
            item.ToTable("lab_order_items");
            item.WithOwner().HasForeignKey("LabOrderId");
            // Khoá riêng là Guid Id (địa chỉ hoá mục khi nhập kết quả).
            item.HasKey(x => x.Id);
            item.Property(x => x.Id).ValueGeneratedNever();

            item.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
            item.Property(x => x.UnitPrice).HasColumnType("numeric(18,2)").IsRequired();
            item.Property(x => x.ResultText).HasMaxLength(4000);
            item.Property(x => x.Conclusion).HasMaxLength(1000);
            item.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            item.HasIndex(x => x.ServicePriceId);
        });

        builder.Navigation(o => o.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
