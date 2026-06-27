using Clinic.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SpecializationsController : ControllerBase
{
    private readonly ClinicDbContext _context;

    public SpecializationsController(ClinicDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetSpecializations()
    {
        var specializations = await _context.Specializations
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.TranslationKey,
                s.Category
            })
            .ToListAsync();

        return Ok(new { success = true, data = specializations });
    }
}
