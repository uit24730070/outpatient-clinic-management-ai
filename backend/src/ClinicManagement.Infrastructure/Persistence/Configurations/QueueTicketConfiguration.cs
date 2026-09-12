using ClinicManagement.Domain.Appointments;
using ClinicManagement.Domain.Doctors;
using ClinicManagement.Domain.Queue;
using ClinicManagement.Domain.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagement.Infrastructure.Persistence.Configurations;

public sealed class QueueTicketConfiguration : IEntityTypeConfiguration<QueueTicket>
{
    public void Configure(EntityTypeBuilder<QueueTicket> builder)
    {
        builder.ToTable("queue_tickets");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TicketDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(t => t.Number).IsRequired();

        // Trạng thái vé lưu dạng chuỗi (đồng nhất quy ước enum-chuỗi).
        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Lọc theo ngày (+ trạng thái) là truy vấn chính của bảng hàng đợi.
        builder.HasIndex(t => new { t.TicketDate, t.Status });
        builder.HasIndex(t => new { t.TicketDate, t.RoomId });
        builder.HasIndex(t => new { t.TicketDate, t.DoctorId });

        // FK tới Lịch khám (tuỳ chọn — vãng lai null); gỡ lịch đặt NULL.
        builder.HasOne<Appointment>()
            .WithMany()
            .HasForeignKey(t => t.AppointmentId)
            .OnDelete(DeleteBehavior.SetNull);

        // FK tới Phòng (tuỳ chọn).
        builder.HasOne<Room>()
            .WithMany()
            .HasForeignKey(t => t.RoomId)
            .OnDelete(DeleteBehavior.SetNull);

        // FK tới Bác sĩ (tuỳ chọn).
        builder.HasOne<Doctor>()
            .WithMany()
            .HasForeignKey(t => t.DoctorId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
