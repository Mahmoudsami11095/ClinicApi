using Clinic.Application.Interfaces;
using Clinic.Domain.Helpers;

namespace Clinic.Application.Services;

public class MaterialSeedingService : IMaterialSeedingService
{
    private readonly IMaterialRepository _repo;

    public MaterialSeedingService(IMaterialRepository repo)
    {
        _repo = repo;
    }

    public async Task SeedDefaultMaterialsAsync(string clinicId, string doctorId)
    {
        if (string.IsNullOrWhiteSpace(clinicId) || string.IsNullOrWhiteSpace(doctorId))
        {
            return;
        }

        // Check if clinic already has default materials to prevent duplicates
        var existing = await _repo.GetByDoctorAndClinicAsync(doctorId, clinicId);
        if (existing.Any(m => m.IsDefault))
        {
            return; // Already seeded
        }

        var defaultMaterials = DefaultMaterialsCatalog.GetDefaultMaterials(clinicId, doctorId);
        await _repo.AddRangeAsync(defaultMaterials);
    }
}
