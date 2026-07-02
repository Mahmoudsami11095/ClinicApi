using Clinic.Domain.Entities;
using Clinic.Domain.Enums;
using Clinic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BCrypt.Net;
using System.Text.Json;

namespace Clinic.Infrastructure.Seed;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();

        try
        {
            // Safely apply pending database migrations automatically on application startup
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText("startup-error.txt", ex.ToString());
            // Log the error but allow the application to continue starting
        }

        await context.SaveChangesAsync();
    }
}

