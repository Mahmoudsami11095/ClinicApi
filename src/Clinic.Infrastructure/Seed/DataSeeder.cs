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
            // Best Practice: Never automatically run migrations on startup in Production.
            // This prevents concurrent migration collisions and startup crashes on Azure.
            // You should apply migrations manually via SQL scripts in production.
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (env == "Development")
            {
                await context.Database.MigrateAsync();
            }
        }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText("startup-error.txt", ex.ToString());
            // Log the error but allow the application to continue starting
        }

        await context.SaveChangesAsync();
    }
}

