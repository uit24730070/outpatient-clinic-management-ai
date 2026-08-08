using System.Reflection;
using ClinicManagement.Application.Appointments;
using ClinicManagement.Application.Auth;
using ClinicManagement.Application.Doctors;
using ClinicManagement.Application.Encounters;
using ClinicManagement.Application.Patients;
using ClinicManagement.Application.Specialties;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<ISpecialtyService, SpecialtyService>();
        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IEncounterService, EncounterService>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
