using System.Reflection;
using ClinicManagement.Application.Ai;
using ClinicManagement.Application.Appointments;
using ClinicManagement.Application.Assistant;
using ClinicManagement.Application.Assistant.Tools;
using ClinicManagement.Application.Auth;
using ClinicManagement.Application.Common.Ai;
using ClinicManagement.Application.Doctors;
using ClinicManagement.Application.Encounters;
using ClinicManagement.Application.Medications;
using ClinicManagement.Application.Patients;
using ClinicManagement.Application.Pharmacy;
using ClinicManagement.Application.Specialties;
using ClinicManagement.Application.StockReceipts;
using ClinicManagement.Application.StockTransactions;
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
        services.AddScoped<IMedicationService, MedicationService>();
        services.AddScoped<IStockReceiptService, StockReceiptService>();
        services.AddScoped<IStockTransactionService, StockTransactionService>();
        services.AddScoped<IPharmacyAlertService, PharmacyAlertService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IPatientSummaryService, PatientSummaryService>();
        services.AddScoped<IEncounterEmbeddingIndexer, EncounterEmbeddingIndexer>();
        services.AddScoped<IPatientQuestionService, PatientQuestionService>();

        // Trợ lý hội thoại (AI-03): bộ công cụ chỉ-đọc + orchestrator tool-calling.
        services.AddScoped<IAssistantTool, SearchPatientsTool>();
        services.AddScoped<IAssistantTool, ListDoctorsTool>();
        services.AddScoped<IAssistantTool, ListAppointmentsTool>();
        services.AddScoped<IAssistantTool, GetPatientEncountersTool>();
        services.AddScoped<IAssistantService, AssistantService>();
        return services;
    }
}
