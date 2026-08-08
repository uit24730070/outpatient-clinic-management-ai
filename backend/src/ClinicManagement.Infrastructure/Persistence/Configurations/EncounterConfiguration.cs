using ClinicManagement.Domain.Appointments;
using ClinicManagement.Domain.Doctors;
using ClinicManagement.Domain.Encounters;
using ClinicManagement.Domain.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagement.Infrastructure.Persistence.Configurations;

public sealed class EncounterConfiguration : IEntityTypeConfiguration<Encounter>
{
    public void Configure(EntityTypeBuilder<Encounter> builder)
    {
        builder.ToTable("encounters");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Symptoms).HasMaxLength(1000);
        builder.Property(e => e.Diagnosis).HasMaxLength(1000).IsRequired();
        builder.Property(e => e.Notes).HasMaxLength(1000);

        // Enum trạng thái lưu dạng chuỗi (đồng nhất Gender/UserRole/AppointmentStatus).
        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Quan hệ 1–1 với lịch khám: mỗi lịch chỉ có một phiếu.
        builder.HasIndex(e => e.AppointmentId).IsUnique();
        // Lịch sử khám theo bệnh nhân.
        builder.HasIndex(e => e.PatientId);

        // Khoá ngoại tới lịch khám, bệnh nhân, bác sĩ; chặn xoá (vật lý) khi còn phiếu tham chiếu.
        builder.HasOne<Appointment>()
            .WithMany()
            .HasForeignKey(e => e.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(e => e.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Doctor>()
            .WithMany()
            .HasForeignKey(e => e.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Cha–con: owned collection ở bảng riêng, vòng đời gắn chặt phiếu khám (ADR 0006).
        builder.OwnsMany(e => e.PrescriptionItems, item =>
        {
            item.ToTable("prescription_items");
            item.WithOwner().HasForeignKey("EncounterId");
            item.Property<int>("Id");
            item.HasKey("Id");

            item.Property(i => i.DrugName).HasMaxLength(200).IsRequired();
            item.Property(i => i.Dosage).HasMaxLength(100).IsRequired();
            item.Property(i => i.Quantity).IsRequired();
            item.Property(i => i.Instruction).HasMaxLength(300);
        });

        // Đọc/ghi cụm đơn thuốc qua backing field (property chỉ đọc).
        builder.Navigation(e => e.PrescriptionItems)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
