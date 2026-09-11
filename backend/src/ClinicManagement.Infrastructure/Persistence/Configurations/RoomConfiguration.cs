using ClinicManagement.Domain.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagement.Infrastructure.Persistence.Configurations;

public sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("rooms");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Code)
            .HasMaxLength(20)
            .IsRequired();
        builder.HasIndex(r => r.Code).IsUnique();

        builder.Property(r => r.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(r => r.Description).HasMaxLength(500);
    }
}
