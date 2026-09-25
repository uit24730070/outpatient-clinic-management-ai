using System.Linq.Expressions;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Domain.Appointments;
using ClinicManagement.Domain.Billing;
using ClinicManagement.Domain.Clinical;
using ClinicManagement.Domain.Common;
using ClinicManagement.Domain.Doctors;
using ClinicManagement.Domain.Encounters;
using ClinicManagement.Domain.Paraclinical;
using ClinicManagement.Domain.Patients;
using ClinicManagement.Domain.Pharmacy;
using ClinicManagement.Domain.Queue;
using ClinicManagement.Domain.Resources;
using ClinicManagement.Domain.Specialties;
using ClinicManagement.Domain.Users;
using ClinicManagement.Domain.Visits;
using Microsoft.EntityFrameworkCore;

namespace UnitTests.Common;

/// <summary>
/// DbContext tối giản cho unit test (provider InMemory) — không phụ thuộc Infrastructure/Npgsql.
/// Nhân bản global query filter soft delete để kiểm thử hành vi ẩn bản ghi đã xoá.
/// </summary>
public sealed class TestDbContext : DbContext, IAppDbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Specialty> Specialties => Set<Specialty>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<DoctorWorkSchedule> DoctorWorkSchedules => Set<DoctorWorkSchedule>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Encounter> Encounters => Set<Encounter>();
    public DbSet<Medication> Medications => Set<Medication>();
    public DbSet<MedicationBatch> MedicationBatches => Set<MedicationBatch>();
    public DbSet<StockReceipt> StockReceipts => Set<StockReceipt>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<ServicePrice> ServicePrices => Set<ServicePrice>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<LabOrder> LabOrders => Set<LabOrder>();
    public DbSet<ClinicManagement.Domain.Clinical.Vitals> Vitals => Set<ClinicManagement.Domain.Clinical.Vitals>();
    public DbSet<QueueTicket> QueueTickets => Set<QueueTicket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Cấu hình cha–con owned collection (Infrastructure không được nạp trong test).
        modelBuilder.Entity<Encounter>(builder =>
        {
            builder.OwnsMany(e => e.PrescriptionItems, item =>
            {
                item.WithOwner().HasForeignKey("EncounterId");
                item.Property<int>("Id");
                item.HasKey("Id");
            });
            builder.Navigation(e => e.PrescriptionItems)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        // Phiếu nhập kho: owned collection dòng nhập (như PrescriptionItem).
        modelBuilder.Entity<StockReceipt>(builder =>
        {
            builder.OwnsMany(r => r.Items, item =>
            {
                item.WithOwner().HasForeignKey("StockReceiptId");
                item.Property<int>("Id");
                item.HasKey("Id");
            });
            builder.Navigation(r => r.Items)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        // Hoá đơn: owned collection dòng hoá đơn (như PrescriptionItem/StockReceiptItem).
        modelBuilder.Entity<Invoice>(builder =>
        {
            builder.OwnsMany(i => i.Items, item =>
            {
                item.WithOwner().HasForeignKey("InvoiceId");
                item.Property<int>("Id");
                item.HasKey("Id");
            });
            builder.Navigation(i => i.Items)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        // Phiếu chỉ định CLS: owned collection mục chỉ định, khoá riêng là Guid Id của mục.
        modelBuilder.Entity<LabOrder>(builder =>
        {
            builder.Ignore(o => o.TotalAmount);
            builder.OwnsMany(o => o.Items, item =>
            {
                item.WithOwner().HasForeignKey("LabOrderId");
                item.HasKey(x => x.Id);
                item.Property(x => x.Id).ValueGeneratedNever();

                item.OwnsMany(x => x.Parameters, p =>
                {
                    p.WithOwner().HasForeignKey("LabOrderItemId");
                    p.Property<int>("Id");
                    p.HasKey("Id");
                });
                item.Navigation(x => x.Parameters)
                    .UsePropertyAccessMode(PropertyAccessMode.Field);
            });
            builder.Navigation(o => o.Items)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
                continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var body = Expression.Not(
                Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted)));
            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(Expression.Lambda(body, parameter));
        }

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>Tạo một <see cref="TestDbContext"/> với CSDL InMemory riêng biệt cho mỗi test.</summary>
    public static TestDbContext CreateInMemory() => CreateInMemory(Guid.NewGuid().ToString());

    /// <summary>Tạo <see cref="TestDbContext"/> trỏ tới một CSDL InMemory theo tên (để mở context mới
    /// trên cùng dữ liệu — kiểm chứng trạng thái đã ghi/rollback).</summary>
    public static TestDbContext CreateInMemory(string databaseName)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new TestDbContext(options);
    }
}
