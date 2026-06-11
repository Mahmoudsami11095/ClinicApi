using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentRepository _repo;

    public AppointmentsController(IAppointmentRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var appointments = await _repo.GetAllAsync();
        var dtos = appointments.Select(a => new AppointmentDto
        {
            Id = a.Id, PatientId = a.PatientId, DoctorId = a.DoctorId,
            Date = a.Date, Status = a.Status, Type = a.Type,
            Notes = a.Notes, ClinicId = a.ClinicId
        }).ToList();
        return Ok(new { data = dtos });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AppointmentDto dto)
    {
        var entity = new Appointment
        {
            Id = string.IsNullOrEmpty(dto.Id) ? Guid.NewGuid().ToString() : dto.Id,
            PatientId = dto.PatientId, DoctorId = dto.DoctorId,
            Date = dto.Date, Status = dto.Status, Type = dto.Type,
            Notes = dto.Notes, ClinicId = dto.ClinicId
        };
        await _repo.AddAsync(entity);
        return Ok(new { message = "Success", data = dto });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] AppointmentDto dto)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound(new { message = "Not found" });

        entity.PatientId = dto.PatientId;
        entity.DoctorId = dto.DoctorId;
        entity.Date = dto.Date;
        entity.Status = dto.Status;
        entity.Type = dto.Type;
        entity.Notes = dto.Notes;
        entity.ClinicId = dto.ClinicId;
        await _repo.UpdateAsync(entity);

        return Ok(new { message = "Success", data = dto });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound(new { message = "Not found" });

        await _repo.DeleteAsync(id);
        return Ok(new { message = "Deleted" });
    }
}
