using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/prescriptions")]
[Authorize]
public class PrescriptionsController : ControllerBase
{
    private readonly IPrescriptionRepository _repo;

    public PrescriptionsController(IPrescriptionRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var prescriptions = await _repo.GetAllAsync();
        var dtos = prescriptions.Select(MapToDto).ToList();
        return Ok(new { data = dtos });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PrescriptionDto dto)
    {
        var entity = MapToEntity(dto);
        entity.Id = string.IsNullOrEmpty(dto.Id) ? Guid.NewGuid().ToString() : dto.Id;
        await _repo.AddAsync(entity);
        return Ok(new { message = "Success", data = dto });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] PrescriptionDto dto)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound(new { message = "Not found" });

        entity.AppointmentId = dto.AppointmentId;
        entity.PatientId = dto.PatientId;
        entity.DoctorId = dto.DoctorId;
        entity.Date = dto.Date;
        entity.Notes = dto.Notes;

        entity.Medications.Clear();
        entity.Medications.AddRange(dto.Medications.Select(m => new MedicationItem
        {
            Name = m.Name, Dosage = m.Dosage, Frequency = m.Frequency, Duration = m.Duration
        }));

        await _repo.UpdateAsync(entity);
        return Ok(new { message = "Success", data = MapToDto(entity) });
    }

    private static PrescriptionDto MapToDto(Prescription p) => new()
    {
        Id = p.Id, AppointmentId = p.AppointmentId, PatientId = p.PatientId,
        DoctorId = p.DoctorId, Date = p.Date, Notes = p.Notes,
        Medications = p.Medications.Select(m => new MedicationItemDto
        {
            Name = m.Name, Dosage = m.Dosage, Frequency = m.Frequency, Duration = m.Duration
        }).ToList()
    };

    private static Prescription MapToEntity(PrescriptionDto dto) => new()
    {
        Id = dto.Id, AppointmentId = dto.AppointmentId, PatientId = dto.PatientId,
        DoctorId = dto.DoctorId, Date = dto.Date, Notes = dto.Notes,
        Medications = dto.Medications.Select(m => new MedicationItem
        {
            Name = m.Name, Dosage = m.Dosage, Frequency = m.Frequency, Duration = m.Duration
        }).ToList()
    };
}
