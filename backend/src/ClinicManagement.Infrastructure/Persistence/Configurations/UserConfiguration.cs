using ClinicManagement.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagement.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    // Thời điểm cố định cho dữ liệu seed (HasData yêu cầu giá trị tất định để migration ổn định).
    private static readonly DateTimeOffset SeedTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    // Tài khoản Admin khởi tạo. Mật khẩu: "Admin@123".
    // BCrypt có salt ngẫu nhiên → không băm lúc chạy migration; dán hash tất định băm sẵn.
    private static readonly Guid AdminId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private const string AdminPasswordHash = "$2a$11$jiUT6V2fXbVVwsuwMwjyoeG8hbGme4uOuG3P5nz9uAi4gHOIsoyTC";

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username)
            .HasMaxLength(100)
            .IsRequired();
        builder.HasIndex(u => u.Username).IsUnique();

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(u => u.Email).HasMaxLength(200);
        builder.Property(u => u.IsActive).IsRequired();

        builder.HasData(new
        {
            Id = AdminId,
            Username = "admin",
            PasswordHash = AdminPasswordHash,
            FullName = "Quản trị hệ thống",
            Role = UserRole.Admin,
            Email = (string?)"admin@clinic.vn",
            IsActive = true,
            CreatedAt = SeedTime,
            IsDeleted = false
        });
    }
}
