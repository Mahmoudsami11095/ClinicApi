using Clinic.Domain.Entities;

namespace Clinic.Application.Interfaces;

public interface IMaterialAlertService
{
    Task CheckAndTriggerLowStockAlertAsync(Material material);
}
