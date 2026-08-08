using ClinicManagement.Domain.Appointments;
using ClinicManagement.Domain.Doctors;
using ClinicManagement.Domain.Patients;
using ClinicManagement.Domain.Specialties;
using ClinicManagement.Domain.Users;
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
    DbSet<User> Users { get; }
    DbSet<Appointment> Appointments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
