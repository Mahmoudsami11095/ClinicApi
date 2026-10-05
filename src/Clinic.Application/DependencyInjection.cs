using Clinic.Application.Interfaces;
using Clinic.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Clinic.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IRadiologyService, RadiologyService>();
        services.AddScoped<IMaterialAlertService, MaterialAlertService>();
        services.AddScoped<IMaterialSeedingService, MaterialSeedingService>();
        
        return services;
    }
}
