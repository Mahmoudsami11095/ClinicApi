using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/patients")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _service;

    public PatientsController(IPatientService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var doctorIdClaim = User.FindFirst("doctorId")?.Value;
        var clinicIdClaim = User.FindFirst("clinicId")?.Value;
        var dtos = await _service.GetAllAsync(doctorIdClaim, clinicIdClaim);
        return Ok(new { data = dtos });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PatientDto dto)
    {
        var doctorIdClaim = User.FindFirst("doctorId")?.Value;
        var clinicIdClaim = User.FindFirst("clinicId")?.Value;
        try
        {
            await _service.CreateAsync(dto, doctorIdClaim, clinicIdClaim);
            return Ok(new { message = "Success" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] PatientDto dto)
    {
        var doctorIdClaim = User.FindFirst("doctorId")?.Value;
        var clinicIdClaim = User.FindFirst("clinicId")?.Value;
        try
        {
            await _service.UpdateAsync(id, dto, doctorIdClaim, clinicIdClaim);
            return Ok(new { message = "Success" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var doctorIdClaim = User.FindFirst("doctorId")?.Value;
        var clinicIdClaim = User.FindFirst("clinicId")?.Value;
        try
        {
            await _service.DeleteAsync(id, doctorIdClaim, clinicIdClaim);
            return Ok(new { message = "Success" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }
}
