using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Clinic.Infrastructure.Repositories;

public class MaterialRepository : IMaterialRepository
{
    private readonly ClinicDbContext _context;

    public MaterialRepository(ClinicDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Material>> GetAllAsync()
    {
        return await _context.Materials.ToListAsync();
    }

    public async Task<IEnumerable<Material>> GetLowStockAsync(string? clinicId = null, string? doctorId = null)
    {
        var query = _context.Materials.Where(m => m.Quantity <= m.MinStockAlert);
        if (!string.IsNullOrEmpty(clinicId) && clinicId != "all")
        {
            query = query.Where(m => m.ClinicId == clinicId);
        }
        if (!string.IsNullOrEmpty(doctorId))
        {
            query = query.Where(m => m.DoctorId == doctorId);
        }
        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Material>> GetExpiredAsync(string? clinicId = null, string? doctorId = null)
    {
        var now = DateTime.UtcNow.Date;
        var query = _context.Materials.Where(m => m.ExpirationDate != null && m.ExpirationDate.Value < now);
        if (!string.IsNullOrEmpty(clinicId) && clinicId != "all")
        {
            query = query.Where(m => m.ClinicId == clinicId);
        }
        if (!string.IsNullOrEmpty(doctorId))
        {
            query = query.Where(m => m.DoctorId == doctorId);
        }
        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Material>> GetByDoctorIdAsync(string doctorId)
    {
        return await _context.Materials
            .Where(m => m.DoctorId == doctorId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Material>> GetByDoctorAndClinicAsync(string doctorId, string clinicId)
    {
        return await _context.Materials
            .Where(m => m.DoctorId == doctorId && m.ClinicId == clinicId)
            .ToListAsync();
    }

    public async Task<Material?> GetByIdAsync(string id)
    {
        return await _context.Materials.FindAsync(id);
    }

    public async Task AddAsync(Material material)
    {
        await _context.Materials.AddAsync(material);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Material> materials)
    {
        ArgumentNullException.ThrowIfNull(materials);
        await _context.Materials.AddRangeAsync(materials).ConfigureAwait(false);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task UpdateAsync(Material material)
    {
        _context.Materials.Update(material);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var material = await GetByIdAsync(id);
        if (material != null)
        {
            _context.Materials.Remove(material);
            await _context.SaveChangesAsync();
        }
    }
}
