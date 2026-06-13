using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/doctors")]
[Authorize]
public class DoctorsController : ControllerBase
{
    private readonly IDoctorRepository _repo;

    public DoctorsController(IDoctorRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var doctors = await _repo.GetAllAsync();
        var dtos = doctors.Select(MapToDto).ToList();
        return Ok(new { data = dtos });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DoctorDto dto)
    {
        var clinicIds = dto.ClinicIds ?? new List<string>();
        var entity = new Doctor
        {
            Id = string.IsNullOrEmpty(dto.Id) ? Guid.NewGuid().ToString() : dto.Id,
            FirstName = dto.FirstName, LastName = dto.LastName,
            Specialization = dto.Specialization, Email = dto.Email,
            ContactNumber = dto.ContactNumber, Avatar = dto.Avatar,
            AvailabilityDays = JsonSerializer.Serialize(dto.Availability?.Days ?? new List<string>()),
            AvailabilityHours = dto.Availability?.Hours ?? ""
        };
        await _repo.AddWithClinicsAsync(entity, clinicIds);
        return Ok(new { message = "Success" });
    }

    private static DoctorDto MapToDto(Doctor d)
    {
        List<string> days;
        try { days = JsonSerializer.Deserialize<List<string>>(d.AvailabilityDays) ?? new(); }
        catch { days = new List<string>(); }

        var clinicAvails = d.DoctorClinics?.Select(dc => {
            List<string> cDays;
            try { cDays = !string.IsNullOrEmpty(dc.AvailabilityDays) ? JsonSerializer.Deserialize<List<string>>(dc.AvailabilityDays) ?? new() : new(); }
            catch { cDays = new List<string>(); }

            return new DoctorClinicAvailabilityDto
            {
                ClinicId = dc.ClinicId,
                AvailabilityHours = dc.AvailabilityHours ?? "",
                AvailabilityDays = cDays
            };
        }).ToList();

        return new DoctorDto
        {
            Id = d.Id, FirstName = d.FirstName, LastName = d.LastName,
            Specialization = d.Specialization, Email = d.Email,
            ContactNumber = d.ContactNumber, Avatar = d.Avatar,
            Availability = new DoctorAvailabilityDto { Days = days, Hours = d.AvailabilityHours },
            ClinicIds = d.DoctorClinics?.Select(dc => dc.ClinicId).ToList(),
            ClinicAvailabilities = clinicAvails
        };
    }
}
