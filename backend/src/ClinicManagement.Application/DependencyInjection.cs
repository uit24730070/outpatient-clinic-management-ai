using System.Reflection;
using ClinicManagement.Application.Patients;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddScoped<IPatientService, PatientService>();
        return services;
    }
}
