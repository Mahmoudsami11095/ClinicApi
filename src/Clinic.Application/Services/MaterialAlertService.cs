using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Clinic.Domain.Enums;

namespace Clinic.Application.Services;

public class MaterialAlertService : IMaterialAlertService
{
    private readonly INotificationService _notificationService;
    private readonly IUserRepository _userRepo;

    public MaterialAlertService(INotificationService notificationService, IUserRepository userRepo)
    {
        _notificationService = notificationService;
        _userRepo = userRepo;
    }

    public async Task CheckAndTriggerLowStockAlertAsync(Material material)
    {
        if (material == null || material.Quantity > material.MinStockAlert)
        {
            return;
        }

        try
        {
            var users = await _userRepo.GetAllAsync();
            var recipientIds = new HashSet<string>();

            // 1. Doctor / Clinic Owner
            if (!string.IsNullOrEmpty(material.DoctorId))
            {
                var docUsers = users.Where(u => u.DoctorId == material.DoctorId);
                foreach (var du in docUsers)
                {
                    recipientIds.Add(du.Id);
                }
            }

            // 2. Assistant, Doctor, and Admin users associated with the material's clinic
            if (!string.IsNullOrEmpty(material.ClinicId))
            {
                var clinicUsers = users.Where(u =>
                    (u.Role == UserRole.Assistant || u.Role == UserRole.Doctor || u.Role == UserRole.Admin) &&
                    (u.ClinicId == material.ClinicId || (u.UserClinics != null && u.UserClinics.Any(uc => uc.ClinicId == material.ClinicId))));

                foreach (var cu in clinicUsers)
                {
                    recipientIds.Add(cu.Id);
                }
            }
            else
            {
                // Fallback if no clinic is specified: notify assistants and admins
                var staffUsers = users.Where(u => u.Role == UserRole.Assistant || u.Role == UserRole.Admin);
                foreach (var su in staffUsers)
                {
                    recipientIds.Add(su.Id);
                }
            }

            var title = "Low Stock Alert";
            var message = $"Material '{material.Name}' is low on stock ({material.Quantity} {material.Unit} remaining, minimum threshold is {material.MinStockAlert}).";
            var type = "Inventory";

            foreach (var recipientId in recipientIds)
            {
                await _notificationService.CreateNotificationAsync(recipientId, title, message, type);
            }
        }
        catch
        {
            // Fail-safe to avoid disrupting core inventory operations
        }
    }
}
