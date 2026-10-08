using System.Threading.Tasks;
using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/cds")]
[Authorize(Roles = "admin,doctor,assistant")]
public class CdsController : ControllerBase
{
    private readonly ICdsEngineService _cdsService;

    public CdsController(ICdsEngineService cdsService)
    {
        _cdsService = cdsService;
    }

    [HttpPost("evaluate")]
    public async Task<IActionResult> EvaluateSafety([FromBody] CdsEvaluationRequestDto request)
    {
        var result = await _cdsService.EvaluatePrescriptionSafetyAsync(request);
        return Ok(new { data = result });
    }

    [HttpGet("rules")]
    public async Task<IActionResult> GetRules()
    {
        var rules = await _cdsService.GetInteractionRulesAsync();
        return Ok(new { data = rules });
    }

    [HttpGet("pediatric-dose")]
    public IActionResult CalculatePediatricDose(
        [FromQuery] string drugName,
        [FromQuery] double weightKg,
        [FromQuery] int ageYears = 8)
    {
        if (string.IsNullOrWhiteSpace(drugName) || weightKg <= 0)
        {
            return BadRequest(new { message = "Drug name and valid patient weight (kg) are required." });
        }

        var result = _cdsService.CalculatePediatricDose(drugName, weightKg, ageYears);
        return Ok(new { data = result });
    }
}
