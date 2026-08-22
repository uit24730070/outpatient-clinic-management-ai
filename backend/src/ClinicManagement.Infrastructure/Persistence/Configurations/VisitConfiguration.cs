using ClinicManagement.Domain.Patients;
using ClinicManagement.Domain.Visits;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagement.Infrastructure.Persistence.Configurations;

public sealed class VisitConfiguration : IEntityTypeConfiguration<Visit>
{
    public void Configure(EntityTypeBuilder<Visit> builder)
    {
        builder.ToTable("visits");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Code).HasMaxLength(20).IsRequired();
        builder.HasIndex(v => v.Code).IsUnique();

        builder.Property(v => v.Note).HasMaxLength(500);

        builder.Property(v => v.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(v => v.PatientId);

        // Chặn xoá (vật lý) bệnh nhân khi còn lượt tham chiếu.
        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(v => v.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
