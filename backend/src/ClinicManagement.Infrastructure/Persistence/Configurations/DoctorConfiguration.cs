using ClinicManagement.Domain.Doctors;
using ClinicManagement.Domain.Specialties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagement.Infrastructure.Persistence.Configurations;

public sealed class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("doctors");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Code)
            .HasMaxLength(20)
            .IsRequired();
        builder.HasIndex(d => d.Code).IsUnique();

        builder.Property(d => d.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(d => d.PhoneNumber).HasMaxLength(20);
        builder.Property(d => d.Email).HasMaxLength(200);

        builder.HasIndex(d => d.FullName);
        builder.HasIndex(d => d.SpecialtyId);

        // Khoá ngoại tới Chuyên khoa; chặn xoá khoa khi còn bác sĩ tham chiếu.
        builder.HasOne<Specialty>()
            .WithMany()
            .HasForeignKey(d => d.SpecialtyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
