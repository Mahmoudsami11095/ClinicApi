using Clinic.Domain.Entities;

namespace Clinic.Application.Interfaces;

public interface IEquipmentRepository
{
    Task<IEnumerable<Equipment>> GetAllAsync();
    Task<IEnumerable<Equipment>> GetByClinicIdAsync(string clinicId);
    Task<IEnumerable<Equipment>> GetByClinicIdsAsync(IEnumerable<string> clinicIds);
    Task<Equipment?> GetByIdAsync(string id);
    Task AddAsync(Equipment equipment);
    Task AddRangeAsync(IEnumerable<Equipment> equipmentList);
    Task UpdateAsync(Equipment equipment);
    Task DeleteAsync(string id);
}
