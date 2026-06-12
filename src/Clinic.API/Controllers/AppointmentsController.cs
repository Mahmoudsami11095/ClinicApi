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
    private readonly IClinicRepository _clinicRepo;

    public AppointmentsController(IAppointmentRepository repo, IClinicRepository clinicRepo)
    {
        _repo = repo;
        _clinicRepo = clinicRepo;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var appointments = await _repo.GetAllAsync();
        var doctorIdClaim = User.FindFirst("doctorId")?.Value;
        if (!string.IsNullOrEmpty(doctorIdClaim))
        {
            var clinics = await _clinicRepo.GetAllAsync();
            var allowedClinicIds = clinics
                .Where(c => c.CreatorDoctorId == doctorIdClaim || 
                            c.DoctorClinics.Any(dc => dc.DoctorId == doctorIdClaim && dc.Status == "Accepted"))
                .Select(c => c.Id)
                .ToList();
            appointments = appointments.Where(a => allowedClinicIds.Contains(a.ClinicId ?? "")).ToList();
        }

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
        var doctorIdClaim = User.FindFirst("doctorId")?.Value;
        if (!string.IsNullOrEmpty(doctorIdClaim))
        {
            var clinics = await _clinicRepo.GetAllAsync();
            var isAllowed = clinics.Any(c => c.Id == dto.ClinicId && 
                (c.CreatorDoctorId == doctorIdClaim || 
                 c.DoctorClinics.Any(dc => dc.DoctorId == doctorIdClaim && dc.Status == "Accepted")));
            if (!isAllowed)
                return StatusCode(403, new { message = "You can only manage appointments for your clinics" });
        }

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

        var doctorIdClaim = User.FindFirst("doctorId")?.Value;
        if (!string.IsNullOrEmpty(doctorIdClaim))
        {
            var clinics = await _clinicRepo.GetAllAsync();
            var isAllowed = clinics.Any(c => c.Id == entity.ClinicId && 
                (c.CreatorDoctorId == doctorIdClaim || 
                 c.DoctorClinics.Any(dc => dc.DoctorId == doctorIdClaim && dc.Status == "Accepted")));
            if (!isAllowed)
                return StatusCode(403, new { message = "You can only manage appointments for your clinics" });
        }

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

        var doctorIdClaim = User.FindFirst("doctorId")?.Value;
        if (!string.IsNullOrEmpty(doctorIdClaim))
        {
            var clinics = await _clinicRepo.GetAllAsync();
            var isAllowed = clinics.Any(c => c.Id == entity.ClinicId && 
                (c.CreatorDoctorId == doctorIdClaim || 
                 c.DoctorClinics.Any(dc => dc.DoctorId == doctorIdClaim && dc.Status == "Accepted")));
            if (!isAllowed)
                return StatusCode(403, new { message = "You can only manage appointments for your clinics" });
        }

        await _repo.DeleteAsync(id);
        return Ok(new { message = "Deleted" });
    }
}
