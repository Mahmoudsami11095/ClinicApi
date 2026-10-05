using Clinic.Application.DTOs;
using Clinic.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/search")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly ClinicDbContext _context;

    public SearchController(ClinicDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> QuickSearch([FromQuery] string? q, [FromQuery] int limit = 8)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Ok(new SearchResultDto());
        }

        var term = q.Trim().ToLower();
        var max = Math.Clamp(limit, 1, 20);

        var patients = await _context.Patients
            .AsNoTracking()
            .Where(p => p.FirstName.ToLower().Contains(term) || 
                        p.LastName.ToLower().Contains(term) || 
                        (p.PhoneNumber != null && p.PhoneNumber.Contains(term)))
            .OrderBy(p => p.FirstName)
            .ThenBy(p => p.LastName)
            .Take(max)
            .Select(p => new SearchPatientItemDto
            {
                Id = p.Id,
                Name = (p.FirstName + " " + p.LastName).Trim(),
                Phone = p.PhoneNumber ?? "",
                Gender = p.Gender,
                ClinicId = p.ClinicId
            })
            .ToListAsync();

        var doctors = await _context.Doctors
            .AsNoTracking()
            .Where(d => d.FirstName.ToLower().Contains(term) || 
                        d.LastName.ToLower().Contains(term) || 
                        (d.Specialization != null && d.Specialization.ToLower().Contains(term)))
            .OrderBy(d => d.FirstName)
            .ThenBy(d => d.LastName)
            .Take(max)
            .Select(d => new SearchDoctorItemDto
            {
                Id = d.Id,
                Name = (d.FirstName + " " + d.LastName).Trim(),
                Specialization = d.Specialization,
                Email = d.Email
            })
            .ToListAsync();

        var chairs = await _context.ClinicChairs
            .AsNoTracking()
            .Where(c => c.ChairName.ToLower().Contains(term) || c.RoomNumber.ToLower().Contains(term))
            .OrderBy(c => c.RoomNumber)
            .Take(max)
            .Select(c => new SearchChairItemDto
            {
                Id = c.Id,
                RoomNumber = c.RoomNumber,
                ChairName = c.ChairName,
                Status = c.Status
            })
            .ToListAsync();

        var materials = await _context.Materials
            .AsNoTracking()
            .Where(m => m.Name.ToLower().Contains(term) || (m.Category != null && m.Category.ToLower().Contains(term)))
            .OrderBy(m => m.Name)
            .Take(max)
            .Select(m => new SearchMaterialItemDto
            {
                Id = m.Id,
                Name = m.Name,
                Category = m.Category ?? "General",
                Quantity = m.Quantity,
                Unit = m.Unit
            })
            .ToListAsync();

        var result = new SearchResultDto
        {
            Patients = patients,
            Doctors = doctors,
            Chairs = chairs,
            Materials = materials
        };

        return Ok(result);
    }
}
