using System.Threading.Tasks;
using Clinic.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/executive/analytics")]
[Authorize(Roles = "admin")]
public class ExecutiveAnalyticsController : ControllerBase
{
    private readonly IExecutiveAnalyticsService _analyticsService;

    public ExecutiveAnalyticsController(IExecutiveAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] bool refresh = false)
    {
        var summary = await _analyticsService.GetNetworkSummaryAsync(refresh);
        return Ok(new { data = summary });
    }

    [HttpGet("branches")]
    public async Task<IActionResult> GetBranches([FromQuery] bool refresh = false)
    {
        var branches = await _analyticsService.GetBranchBenchmarksAsync(refresh);
        return Ok(new { data = branches });
    }

    [HttpGet("doctors")]
    public async Task<IActionResult> GetDoctors([FromQuery] bool refresh = false)
    {
        var doctors = await _analyticsService.GetDoctorProductivityAsync(refresh);
        return Ok(new { data = doctors });
    }

    [HttpGet("supply-velocity")]
    public async Task<IActionResult> GetSupplyVelocity([FromQuery] bool refresh = false)
    {
        var supply = await _analyticsService.GetSupplyChainVelocityAsync(refresh);
        return Ok(new { data = supply });
    }

    [HttpPost("refresh-cache")]
    public IActionResult RefreshCache()
    {
        _analyticsService.InvalidateCache();
        return Ok(new { message = "Executive analytics cache invalidated successfully." });
    }
}
