using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Clinic.Infrastructure.Repositories;

public class EquipmentRepository : IEquipmentRepository
{
    private readonly ClinicDbContext _context;

    public EquipmentRepository(ClinicDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Equipment>> GetAllAsync()
    {
        return await _context.Equipment
            .OrderBy(e => e.Category)
            .ThenBy(e => e.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Equipment>> GetByClinicIdAsync(string clinicId)
    {
        return await _context.Equipment
            .Where(e => e.ClinicId == clinicId)
            .OrderBy(e => e.Category)
            .ThenBy(e => e.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Equipment>> GetByClinicIdsAsync(IEnumerable<string> clinicIds)
    {
        return await _context.Equipment
            .Where(e => clinicIds.Contains(e.ClinicId))
            .OrderBy(e => e.Category)
            .ThenBy(e => e.Name)
            .ToListAsync();
    }

    public async Task<Equipment?> GetByIdAsync(string id)
    {
        return await _context.Equipment.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task AddAsync(Equipment equipment)
    {
        await _context.Equipment.AddAsync(equipment);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Equipment> equipmentList)
    {
        ArgumentNullException.ThrowIfNull(equipmentList);
        await _context.Equipment.AddRangeAsync(equipmentList);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Equipment equipment)
    {
        _context.Equipment.Update(equipment);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var item = await GetByIdAsync(id);
        if (item != null)
        {
            _context.Equipment.Remove(item);
            await _context.SaveChangesAsync();
        }
    }
}
