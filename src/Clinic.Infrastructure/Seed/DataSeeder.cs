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

            // Auto-promote the specified super admin email to the Admin role if they exist
            var user = await context.Users.FirstOrDefaultAsync(u => u.Email == "mahmoudsami11095@gmail.com");
            if (user != null && user.Role != UserRole.Admin)
            {
                user.Role = UserRole.Admin;
                context.Users.Update(user);
                await context.SaveChangesAsync();
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

