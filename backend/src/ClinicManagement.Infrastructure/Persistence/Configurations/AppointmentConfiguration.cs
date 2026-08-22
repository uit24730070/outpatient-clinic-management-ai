using ClinicManagement.Domain.Appointments;
using ClinicManagement.Domain.Doctors;
using ClinicManagement.Domain.Patients;
using ClinicManagement.Domain.Visits;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagement.Infrastructure.Persistence.Configurations;

public sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("appointments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.StartTime).IsRequired();
        builder.Property(a => a.EndTime).IsRequired();
        builder.Property(a => a.Reason).HasMaxLength(500);

        // Dịch vụ khám đăng ký lúc đặt lịch (snapshot tên/giá) — tuỳ chọn (ADR 0016).
        builder.Property(a => a.ServiceName).HasMaxLength(200);
        builder.Property(a => a.ServicePrice).HasColumnType("numeric(18,2)");

        // Enum trạng thái lưu dạng chuỗi (đồng nhất Gender/UserRole).
        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Hỗ trợ lọc hàng đợi theo bác sĩ + thời gian, và tra theo bệnh nhân.
        builder.HasIndex(a => new { a.DoctorId, a.StartTime });
        builder.HasIndex(a => a.PatientId);

        // Lượt tiếp đón gom lịch (ADR 0017) — nullable, hỗ trợ gom lịch theo lượt.
        builder.HasIndex(a => a.VisitId);
        builder.HasOne<Visit>()
            .WithMany()
            .HasForeignKey(a => a.VisitId)
            .OnDelete(DeleteBehavior.Restrict);

        // Khoá ngoại kép; chặn xoá (vật lý) bệnh nhân/bác sĩ khi còn lịch tham chiếu.
        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Doctor>()
            .WithMany()
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
