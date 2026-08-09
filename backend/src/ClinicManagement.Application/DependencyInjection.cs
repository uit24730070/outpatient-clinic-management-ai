using System.Reflection;
using ClinicManagement.Application.Ai;
using ClinicManagement.Application.Appointments;
using ClinicManagement.Application.Auth;
using ClinicManagement.Application.Doctors;
using ClinicManagement.Application.Encounters;
using ClinicManagement.Application.Patients;
using ClinicManagement.Application.Specialties;
using ClinicManagement.Application.Users;
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
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IPatientSummaryService, PatientSummaryService>();
        services.AddScoped<IEncounterEmbeddingIndexer, EncounterEmbeddingIndexer>();
        services.AddScoped<IPatientQuestionService, PatientQuestionService>();
        return services;
    }
}
