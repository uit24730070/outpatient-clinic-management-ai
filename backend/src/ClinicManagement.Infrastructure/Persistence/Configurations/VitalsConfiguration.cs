using ClinicManagement.Domain.Appointments;
using ClinicManagement.Domain.Clinical;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagement.Infrastructure.Persistence.Configurations;

public sealed class VitalsConfiguration : IEntityTypeConfiguration<Vitals>
{
    public void Configure(EntityTypeBuilder<Vitals> builder)
    {
        builder.ToTable("vitals");

        builder.HasKey(v => v.Id);

        // Một bộ sinh hiệu cho mỗi lượt khám (1–1).
        builder.HasIndex(v => v.AppointmentId).IsUnique();

        // BMI là thuộc tính tính toán — không lưu cột.
        builder.Ignore(v => v.Bmi);

        builder.Property(v => v.HeightCm).HasColumnType("numeric(5,2)");
        builder.Property(v => v.WeightKg).HasColumnType("numeric(5,2)");
        builder.Property(v => v.TemperatureC).HasColumnType("numeric(4,1)");
        builder.Property(v => v.Notes).HasMaxLength(1000);

        // FK tới Lịch khám; xoá (vật lý) lịch bị chặn khi còn sinh hiệu.
        builder.HasOne<Appointment>()
            .WithMany()
            .HasForeignKey(v => v.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
