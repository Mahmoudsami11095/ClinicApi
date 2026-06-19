using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/doctors")]
[Authorize]
public class DoctorsController : ControllerBase
{
    private readonly IDoctorService _service;

    public DoctorsController(IDoctorService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var dtos = await _service.GetAllAsync();
        return Ok(new { data = dtos });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DoctorDto dto)
    {
        await _service.CreateAsync(dto);
        return Ok(new { message = "Success" });
    }
}
