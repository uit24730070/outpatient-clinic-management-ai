using ClinicManagement.Domain.Appointments;
using ClinicManagement.Domain.Billing;
using ClinicManagement.Domain.Doctors;
using ClinicManagement.Domain.Encounters;
using ClinicManagement.Domain.Paraclinical;
using ClinicManagement.Domain.Patients;
using ClinicManagement.Domain.Pharmacy;
using ClinicManagement.Domain.Resources;
using ClinicManagement.Domain.Specialties;
using ClinicManagement.Domain.Users;
using ClinicManagement.Domain.Visits;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.Common.Interfaces;

/// <summary>
/// Trừu tượng của DbContext cho lớp Application, giúp tách khỏi hiện thực EF Core cụ thể.
/// </summary>
public interface IAppDbContext
{
    DbSet<Patient> Patients { get; }
    DbSet<Specialty> Specialties { get; }
    DbSet<Doctor> Doctors { get; }
    DbSet<DoctorWorkSchedule> DoctorWorkSchedules { get; }
    DbSet<Room> Rooms { get; }
    DbSet<User> Users { get; }
    DbSet<Visit> Visits { get; }
    DbSet<Appointment> Appointments { get; }
    DbSet<Encounter> Encounters { get; }
    DbSet<Medication> Medications { get; }
    DbSet<MedicationBatch> MedicationBatches { get; }
    DbSet<StockReceipt> StockReceipts { get; }
    DbSet<StockTransaction> StockTransactions { get; }
    DbSet<ServicePrice> ServicePrices { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<LabOrder> LabOrders { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
