using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/clinics")]
[Authorize]
public class ClinicsController : ControllerBase
{
    private readonly IClinicRepository _repo;

    public ClinicsController(IClinicRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var clinics = await _repo.GetAllAsync();
        var dtos = clinics.Select(c => new ClinicDto
        {
            Id = c.Id, Name = c.Name, Address = c.Address, Phone = c.Phone
        }).ToList();
        return Ok(new { data = dtos });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ClinicDto dto)
    {
        var entity = new ClinicEntity
        {
            Id = string.IsNullOrEmpty(dto.Id) ? Guid.NewGuid().ToString() : dto.Id,
            Name = dto.Name, Address = dto.Address, Phone = dto.Phone
        };
        await _repo.AddAsync(entity);
        var result = new ClinicDto { Id = entity.Id, Name = entity.Name, Address = entity.Address, Phone = entity.Phone };
        return Ok(new { message = "Success", data = result });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] ClinicDto dto)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound(new { message = "Not found" });

        entity.Name = dto.Name;
        entity.Address = dto.Address;
        entity.Phone = dto.Phone;
        await _repo.UpdateAsync(entity);

        var result = new ClinicDto { Id = entity.Id, Name = entity.Name, Address = entity.Address, Phone = entity.Phone };
        return Ok(new { message = "Success", data = result });
    }
}
