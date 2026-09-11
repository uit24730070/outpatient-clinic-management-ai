using ClinicManagement.Domain.Doctors;
using ClinicManagement.Domain.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagement.Infrastructure.Persistence.Configurations;

public sealed class DoctorWorkScheduleConfiguration : IEntityTypeConfiguration<DoctorWorkSchedule>
{
    public void Configure(EntityTypeBuilder<DoctorWorkSchedule> builder)
    {
        builder.ToTable("doctor_work_schedules");

        builder.HasKey(s => s.Id);

        // Thứ trong tuần lưu dạng chuỗi (đồng nhất quy ước enum-chuỗi Gender/Status).
        builder.Property(s => s.DayOfWeek)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Giờ địa phương phòng khám → cột time.
        builder.Property(s => s.StartTime).HasColumnType("time").IsRequired();
        builder.Property(s => s.EndTime).HasColumnType("time").IsRequired();

        builder.HasIndex(s => new { s.DoctorId, s.DayOfWeek });

        // FK tới Bác sĩ; xoá (vật lý) bác sĩ bị chặn khi còn khung làm việc.
        builder.HasOne<Doctor>()
            .WithMany()
            .HasForeignKey(s => s.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK tới Phòng (tuỳ chọn); gỡ phòng đặt RoomId về NULL.
        builder.HasOne<Room>()
            .WithMany()
            .HasForeignKey(s => s.RoomId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
