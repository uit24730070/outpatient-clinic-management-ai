using ClinicManagement.Domain.Patients;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.Common.Interfaces;

/// <summary>
/// Trừu tượng của DbContext cho lớp Application, giúp tách khỏi hiện thực EF Core cụ thể.
/// </summary>
public interface IAppDbContext
{
    DbSet<Patient> Patients { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
