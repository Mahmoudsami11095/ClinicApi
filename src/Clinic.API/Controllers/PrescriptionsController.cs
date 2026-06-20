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
    private readonly IAppointmentRepository _appointmentRepo;
    private readonly IClinicRepository _clinicRepo;

    public PrescriptionsController(IPrescriptionRepository repo, IAppointmentRepository appointmentRepo, IClinicRepository clinicRepo)
    {
        _repo = repo;
        _appointmentRepo = appointmentRepo;
        _clinicRepo = clinicRepo;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var prescriptions = await _repo.GetAllAsync();
        var doctorIdClaim = User.FindFirst("doctorId")?.Value;
        var clinicIdClaim = User.FindFirst("clinicId")?.Value;

        if (!string.IsNullOrEmpty(doctorIdClaim) || !string.IsNullOrEmpty(clinicIdClaim))
        {
            var appointments = await _appointmentRepo.GetAllAsync();
            if (!string.IsNullOrEmpty(doctorIdClaim))
            {
                var clinics = await _clinicRepo.GetAllAsync();
                var allowedClinicIds = clinics
                    .Where(c => c.CreatorDoctorId == doctorIdClaim || 
                                c.DoctorClinics.Any(dc => dc.DoctorId == doctorIdClaim && dc.Status == "Accepted"))
                    .Select(c => c.Id)
                    .ToList();
                var allowedApptIds = appointments.Where(a => allowedClinicIds.Contains(a.ClinicId ?? "")).Select(a => a.Id).ToList();
                prescriptions = prescriptions.Where(p => allowedApptIds.Contains(p.AppointmentId)).ToList();
            }
            else if (!string.IsNullOrEmpty(clinicIdClaim))
            {
                var allowedApptIds = appointments.Where(a => a.ClinicId == clinicIdClaim).Select(a => a.Id).ToList();
                prescriptions = prescriptions.Where(p => allowedApptIds.Contains(p.AppointmentId)).ToList();
            }
        }

        var dtos = prescriptions.Select(MapToDto).ToList();
        return Ok(new { data = dtos });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PrescriptionDto dto)
    {
        var doctorIdClaim = User.FindFirst("doctorId")?.Value;
        var clinicIdClaim = User.FindFirst("clinicId")?.Value;

        if (!string.IsNullOrEmpty(doctorIdClaim) || !string.IsNullOrEmpty(clinicIdClaim))
        {
            var appointment = await _appointmentRepo.GetByIdAsync(dto.AppointmentId);
            if (appointment == null) return NotFound(new { message = "Appointment not found" });

            if (!string.IsNullOrEmpty(doctorIdClaim))
            {
                var clinics = await _clinicRepo.GetAllAsync();
                var isAllowed = clinics.Any(c => c.Id == appointment.ClinicId && 
                    (c.CreatorDoctorId == doctorIdClaim || 
                     c.DoctorClinics.Any(dc => dc.DoctorId == doctorIdClaim && dc.Status == "Accepted")));
                if (!isAllowed) return StatusCode(403, new { message = "You can only manage prescriptions for your clinics" });
            }
            else if (!string.IsNullOrEmpty(clinicIdClaim))
            {
                if (appointment.ClinicId != clinicIdClaim)
                    return StatusCode(403, new { message = "You can only manage prescriptions for your assigned clinic" });
            }
        }

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

        var doctorIdClaim = User.FindFirst("doctorId")?.Value;
        var clinicIdClaim = User.FindFirst("clinicId")?.Value;

        if (!string.IsNullOrEmpty(doctorIdClaim) || !string.IsNullOrEmpty(clinicIdClaim))
        {
            var appointment = await _appointmentRepo.GetByIdAsync(dto.AppointmentId);
            var origAppointment = await _appointmentRepo.GetByIdAsync(entity.AppointmentId);

            if (appointment == null || origAppointment == null) return NotFound(new { message = "Appointment not found" });

            if (!string.IsNullOrEmpty(doctorIdClaim))
            {
                var clinics = await _clinicRepo.GetAllAsync();
                var isAllowed = clinics.Any(c => c.Id == appointment.ClinicId && 
                    (c.CreatorDoctorId == doctorIdClaim || 
                     c.DoctorClinics.Any(dc => dc.DoctorId == doctorIdClaim && dc.Status == "Accepted"))) &&
                     clinics.Any(c => c.Id == origAppointment.ClinicId && 
                    (c.CreatorDoctorId == doctorIdClaim || 
                     c.DoctorClinics.Any(dc => dc.DoctorId == doctorIdClaim && dc.Status == "Accepted")));
                if (!isAllowed) return StatusCode(403, new { message = "You can only manage prescriptions for your clinics" });
            }
            else if (!string.IsNullOrEmpty(clinicIdClaim))
            {
                if (appointment.ClinicId != clinicIdClaim || origAppointment.ClinicId != clinicIdClaim)
                    return StatusCode(403, new { message = "You can only manage prescriptions for your assigned clinic" });
            }
        }

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
