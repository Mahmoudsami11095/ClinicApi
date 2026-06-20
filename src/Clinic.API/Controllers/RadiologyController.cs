using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RadiologyController : ControllerBase
{
    private readonly IRadiologyService _radiologyService;

    public RadiologyController(IRadiologyService radiologyService)
    {
        _radiologyService = radiologyService;
    }

    // ── Centers ──
    [HttpGet("centers")]
    public async Task<IActionResult> GetCenters()
    {
        var centers = await _radiologyService.GetCentersAsync();
        return Ok(centers);
    }

    [HttpPost("centers")]
    [Authorize(Roles = "admin,doctor,assistant")]
    public async Task<IActionResult> CreateCenter([FromBody] CreateRadiologyCenterDto dto)
    {
        var center = await _radiologyService.CreateCenterAsync(dto);
        return CreatedAtAction(nameof(GetCenters), new { id = center.Id }, center);
    }

    [HttpPut("centers/{id}")]
    [Authorize(Roles = "admin,doctor,assistant")]
    public async Task<IActionResult> UpdateCenter(string id, [FromBody] CreateRadiologyCenterDto dto)
    {
        var center = await _radiologyService.UpdateCenterAsync(id, dto);
        return Ok(center);
    }

    [HttpDelete("centers/{id}")]
    [Authorize(Roles = "admin,doctor")]
    public async Task<IActionResult> DeleteCenter(string id)
    {
        await _radiologyService.DeleteCenterAsync(id);
        return NoContent();
    }

    // ── Records ──
    [HttpGet("records")]
    public async Task<IActionResult> GetRecords([FromQuery] string? doctorId)
    {
        if (!string.IsNullOrEmpty(doctorId))
        {
            var doctorRecords = await _radiologyService.GetRecordsByDoctorAsync(doctorId);
            return Ok(doctorRecords);
        }
        var records = await _radiologyService.GetRecordsAsync();
        return Ok(records);
    }

    [HttpPost("records")]
    [Authorize(Roles = "admin,doctor,assistant")]
    public async Task<IActionResult> CreateRecord([FromBody] CreateRadiologyRecordDto dto)
    {
        var record = await _radiologyService.CreateRecordAsync(dto);
        return CreatedAtAction(nameof(GetRecords), new { id = record.Id }, record);
    }

    [HttpPut("records/{id}")]
    [Authorize(Roles = "admin,doctor,assistant")]
    public async Task<IActionResult> UpdateRecord(string id, [FromBody] CreateRadiologyRecordDto dto)
    {
        var record = await _radiologyService.UpdateRecordAsync(id, dto);
        return Ok(record);
    }

    [HttpDelete("records/{id}")]
    [Authorize(Roles = "admin,doctor,assistant")]
    public async Task<IActionResult> DeleteRecord(string id)
    {
        await _radiologyService.DeleteRecordAsync(id);
        return NoContent();
    }
}
