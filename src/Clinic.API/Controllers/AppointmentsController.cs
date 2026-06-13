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
    private readonly IDoctorRepository _doctorRepo;

    public AppointmentsController(IAppointmentRepository repo, IClinicRepository clinicRepo, IDoctorRepository doctorRepo)
    {
        _repo = repo;
        _clinicRepo = clinicRepo;
        _doctorRepo = doctorRepo;
    }

    private async Task<string?> ValidateDoctorAvailability(string doctorId, string clinicId, string appointmentDateStr)
    {
        if (string.IsNullOrEmpty(doctorId) || string.IsNullOrEmpty(clinicId) || string.IsNullOrEmpty(appointmentDateStr))
            return null;

        var doctors = await _doctorRepo.GetAllAsync();
        var d = doctors.FirstOrDefault(x => x.Id == doctorId);
        if (d == null)
            return "Doctor not found";

        if (!DateTime.TryParse(appointmentDateStr, out var apptDate))
        {
            return "Invalid appointment date format";
        }

        var dayOfWeek = apptDate.DayOfWeek.ToString();
        var timeOfDay = apptDate.TimeOfDay;

        var dc = d.DoctorClinics.FirstOrDefault(x => x.ClinicId == clinicId);
        
        string? hoursStr = null;
        string? daysStr = null;

        if (dc != null && !string.IsNullOrEmpty(dc.AvailabilityHours))
        {
            hoursStr = dc.AvailabilityHours;
            daysStr = dc.AvailabilityDays;
        }
        else
        {
            hoursStr = d.AvailabilityHours;
            daysStr = d.AvailabilityDays;
        }

        if (string.IsNullOrEmpty(hoursStr))
        {
            return null;
        }

        if (!string.IsNullOrEmpty(daysStr))
        {
            try
            {
                var days = System.Text.Json.JsonSerializer.Deserialize<List<string>>(daysStr);
                if (days != null && days.Any() && !days.Contains(dayOfWeek, StringComparer.OrdinalIgnoreCase))
                {
                    return $"Doctor is not available on {dayOfWeek} at this clinic.";
                }
            }
            catch
            {
            }
        }

        var parts = hoursStr.Split('-');
        if (parts.Length == 2)
        {
            if (TimeSpan.TryParse(parts[0], out var startTime) && TimeSpan.TryParse(parts[1], out var endTime))
            {
                if (timeOfDay < startTime || timeOfDay > endTime)
                {
                    return $"Appointment time {timeOfDay:hh\\:mm} is outside the doctor's availability hours ({hoursStr}) for this clinic.";
                }
            }
        }

        return null;
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

        var validationError = await ValidateDoctorAvailability(dto.DoctorId, dto.ClinicId ?? "", dto.Date);
        if (validationError != null)
        {
            return BadRequest(new { message = validationError });
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

        var validationError = await ValidateDoctorAvailability(dto.DoctorId, dto.ClinicId ?? "", dto.Date);
        if (validationError != null)
        {
            return BadRequest(new { message = validationError });
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
