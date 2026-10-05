namespace Clinic.Application.Interfaces;

public interface IMaterialSeedingService
{
    Task SeedDefaultMaterialsAsync(string clinicId, string doctorId);
}
