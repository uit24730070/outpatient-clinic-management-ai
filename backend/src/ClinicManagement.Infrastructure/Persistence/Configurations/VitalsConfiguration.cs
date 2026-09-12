using ClinicManagement.Domain.Appointments;
using ClinicManagement.Domain.Clinical;
using ClinicManagement.Domain.Visits;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagement.Infrastructure.Persistence.Configurations;

public sealed class VitalsConfiguration : IEntityTypeConfiguration<Vitals>
{
    public void Configure(EntityTypeBuilder<Vitals> builder)
    {
        builder.ToTable("vitals");

        builder.HasKey(v => v.Id);

        // Nhiều lần đo cho cùng một lượt/lịch (lịch sử, không upsert) — index thường để tra nhanh
        // "lần gần nhất"/"lịch sử" theo lượt hoặc theo lịch lẻ, không ràng buộc duy nhất.
        builder.HasIndex(v => v.VisitId);
        builder.HasIndex(v => v.AppointmentId);

        builder.HasOne<Visit>()
            .WithMany()
            .HasForeignKey(v => v.VisitId)
            .OnDelete(DeleteBehavior.Restrict);

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
