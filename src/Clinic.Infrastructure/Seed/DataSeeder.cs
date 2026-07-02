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

            // Auto-promote and restore the specified super admin emails
            var adminEmails = new[] { "mahmoudsami11095@gmail.com", "msami11095@gmail.com" };
            foreach (var email in adminEmails)
            {
                // Use IgnoreQueryFilters to find even soft-deleted accounts
                var user = await context.Users.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Email == email);
                if (user != null)
                {
                    bool changed = false;

                    if (user.IsDeleted)
                    {
                        user.IsDeleted = false;
                        changed = true;
                    }

                    if (user.Role != UserRole.Admin)
                    {
                        user.Role = UserRole.Admin;
                        changed = true;
                    }

                    if (changed)
                    {
                        context.Users.Update(user);
                    }
                }
            }
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText("startup-error.txt", ex.ToString());
            // Log the error but allow the application to continue starting
        }

        await context.SaveChangesAsync();
    }
}

