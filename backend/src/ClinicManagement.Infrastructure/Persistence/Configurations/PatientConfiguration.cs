using ClinicManagement.Domain.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagement.Infrastructure.Persistence.Configurations;

public sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("patients");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code)
            .HasMaxLength(20)
            .IsRequired();
        builder.HasIndex(p => p.Code).IsUnique();

        builder.Property(p => p.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Gender)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.PhoneNumber).HasMaxLength(20);
        builder.Property(p => p.Address).HasMaxLength(500);

        builder.HasIndex(p => p.FullName);
        builder.HasIndex(p => p.PhoneNumber);
    }
}
