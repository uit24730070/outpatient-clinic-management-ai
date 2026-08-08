using ClinicManagement.Domain.Doctors;
using ClinicManagement.Domain.Specialties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagement.Infrastructure.Persistence.Configurations;

public sealed class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    // Thời điểm cố định cho dữ liệu seed (HasData yêu cầu giá trị tất định).
    private static readonly DateTimeOffset SeedTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    // Hồ sơ bác sĩ demo gắn với tài khoản "bacsi" (UserConfiguration.DoctorUserId),
    // thuộc chuyên khoa "Nội tổng quát" đã seed ở SpecialtyConfiguration.
    private static readonly Guid DemoDoctorId = Guid.Parse("d0c70001-0000-0000-0000-000000000001");
    private static readonly Guid NoiTongQuatSpecialtyId = Guid.Parse("11111111-1111-1111-1111-111111111111");

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

        // Liên kết tài khoản đăng nhập ↔ hồ sơ bác sĩ (ADR 0009).
        // Unique nhưng nullable: Postgres cho phép nhiều NULL trong unique index,
        // nên nhiều bác sĩ chưa gắn tài khoản không xung đột; đã gắn thì 1–1.
        builder.HasIndex(d => d.UserId).IsUnique();

        // Khoá ngoại tới Chuyên khoa; chặn xoá khoa khi còn bác sĩ tham chiếu.
        builder.HasOne<Specialty>()
            .WithMany()
            .HasForeignKey(d => d.SpecialtyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Khoá ngoại tới User (không điều hướng ngược); gỡ tài khoản đặt UserId về NULL.
        builder.HasOne<Domain.Users.User>()
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Seed hồ sơ bác sĩ demo gắn tài khoản "bacsi" để minh hoạ "phòng khám của tôi".
        builder.HasData(new
        {
            Id = DemoDoctorId,
            Code = "BS-000001",
            FullName = "Bác sĩ Demo",
            SpecialtyId = NoiTongQuatSpecialtyId,
            PhoneNumber = (string?)null,
            Email = (string?)"bacsi@clinic.vn",
            UserId = (Guid?)UserConfiguration.DoctorUserId,
            CreatedAt = SeedTime,
            IsDeleted = false
        });
    }
}
