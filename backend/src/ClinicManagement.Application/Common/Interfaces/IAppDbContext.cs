using ClinicManagement.Domain.Doctors;
using ClinicManagement.Domain.Patients;
using ClinicManagement.Domain.Specialties;
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

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
