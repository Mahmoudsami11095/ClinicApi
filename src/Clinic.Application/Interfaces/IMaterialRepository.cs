using Clinic.Domain.Entities;

namespace Clinic.Application.Interfaces;

public interface IMaterialRepository
{
    Task<IEnumerable<Material>> GetAllAsync();
    Task<IEnumerable<Material>> GetLowStockAsync(string? clinicId = null, string? doctorId = null);
    Task<IEnumerable<Material>> GetExpiredAsync(string? clinicId = null, string? doctorId = null);
    Task<IEnumerable<Material>> GetByDoctorIdAsync(string doctorId);
    Task<IEnumerable<Material>> GetByDoctorAndClinicAsync(string doctorId, string clinicId);
    Task<Material?> GetByIdAsync(string id);
    Task AddAsync(Material material);
    Task UpdateAsync(Material material);
    Task DeleteAsync(string id);
}
