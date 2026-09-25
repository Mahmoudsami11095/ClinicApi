using Clinic.Application.Common;
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
        var dtos = await _service.GetAllAsync(User.GetDoctorId(), User.GetClinicId());
        return Ok(new { data = dtos });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
            var dto = await _service.GetByIdAsync(id, User.GetDoctorId(), User.GetClinicId());
            if (dto == null) return NotFound();
            return Ok(new { data = dto });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PatientDto dto)
    {
        try
        {
            await _service.CreateAsync(dto, User.GetDoctorId(), User.GetClinicId());
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
        try
        {
            await _service.UpdateAsync(id, dto, User.GetDoctorId(), User.GetClinicId());
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
        try
        {
            await _service.DeleteAsync(id, User.GetDoctorId(), User.GetClinicId());
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
