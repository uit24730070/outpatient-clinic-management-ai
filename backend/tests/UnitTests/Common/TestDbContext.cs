using System.Linq.Expressions;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Domain.Appointments;
using ClinicManagement.Domain.Common;
using ClinicManagement.Domain.Doctors;
using ClinicManagement.Domain.Encounters;
using ClinicManagement.Domain.Patients;
using ClinicManagement.Domain.Pharmacy;
using ClinicManagement.Domain.Specialties;
using ClinicManagement.Domain.Users;
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
    public DbSet<User> Users => Set<User>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Encounter> Encounters => Set<Encounter>();
    public DbSet<Medication> Medications => Set<Medication>();
    public DbSet<MedicationBatch> MedicationBatches => Set<MedicationBatch>();
    public DbSet<StockReceipt> StockReceipts => Set<StockReceipt>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();

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
    public static TestDbContext CreateInMemory()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestDbContext(options);
    }
}
