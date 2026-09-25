using Clinic.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SpecializationsController : ControllerBase
{
    private readonly ClinicDbContext _context;
    private readonly IMemoryCache _cache;
    private const string CacheKey = "Specializations_List";

    public SpecializationsController(ClinicDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    [HttpGet]
    public async Task<IActionResult> GetSpecializations()
    {
        if (!_cache.TryGetValue(CacheKey, out var specializations))
        {
            specializations = await _context.Specializations
                .AsNoTracking()
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.TranslationKey,
                    s.Category
                })
                .ToListAsync();

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromHours(1))
                .SetAbsoluteExpiration(TimeSpan.FromHours(24));

            _cache.Set(CacheKey, specializations, cacheEntryOptions);
        }

        return Ok(new { success = true, data = specializations });
    }
}
