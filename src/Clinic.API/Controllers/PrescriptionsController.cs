using Clinic.Application.Common;
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
        var doctorIdClaim = User.GetDoctorId();
        var clinicIdClaim = User.GetClinicId();

        if (!string.IsNullOrEmpty(doctorIdClaim) || !string.IsNullOrEmpty(clinicIdClaim))
        {
            var appointments = await _appointmentRepo.GetAllAsync();
            if (!string.IsNullOrEmpty(doctorIdClaim))
            {
                var allowedClinicIds = await _clinicRepo.GetAllowedClinicIdsForDoctorAsync(doctorIdClaim);
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

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var prescription = await _repo.GetByIdAsync(id);
        if (prescription == null) return NotFound(new { message = "Prescription not found" });

        return Ok(new { data = MapToDto(prescription) });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PrescriptionDto dto)
    {
        var doctorIdClaim = User.GetDoctorId();
        var clinicIdClaim = User.GetClinicId();

        if (!string.IsNullOrEmpty(doctorIdClaim) || !string.IsNullOrEmpty(clinicIdClaim))
        {
            var appointment = await _appointmentRepo.GetByIdAsync(dto.AppointmentId);
            if (appointment == null) return NotFound(new { message = "Appointment not found" });

            if (!string.IsNullOrEmpty(doctorIdClaim))
            {
                var isAllowed = !string.IsNullOrEmpty(appointment.ClinicId) && 
                                await _clinicRepo.IsDoctorAuthorizedForClinicAsync(doctorIdClaim, appointment.ClinicId);
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

        // If explicitly created as finalized, digitally sign and lock
        if (dto.IsFinalized)
        {
            var doctorName = User.GetUserName() ?? User.Identity?.Name ?? "Attending Physician";
            entity.IsFinalized = true;
            entity.Status = "finalized";
            entity.FinalizedAt = string.IsNullOrEmpty(dto.FinalizedAt) ? DateTime.UtcNow.ToString("o") : dto.FinalizedAt;
            entity.DigitalSignature = string.IsNullOrEmpty(dto.DigitalSignature) 
                ? $"Digitally Signed by Dr. {doctorName} on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC (Verified)" 
                : dto.DigitalSignature;
        }

        await _repo.AddAsync(entity);
        return Ok(new { message = "Success", data = MapToDto(entity) });
    }

    /// <summary>
    /// BR-RX-02: Prescription Immutability & Audit Lock
    /// Once finalized, editing medications or details is forbidden.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] PrescriptionDto dto)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound(new { message = "Not found" });

        // BR-RX-02: Strict immutability check
        if (entity.IsFinalized || entity.Status == "finalized" || entity.Status == "superseded")
        {
            return BadRequest(new
            {
                message = "BR-RX-02: Finalized prescriptions are legally locked and read-only. Modifications are forbidden. You must issue a superseding prescription to modify medications."
            });
        }

        var doctorIdClaim = User.GetDoctorId();
        var clinicIdClaim = User.GetClinicId();

        if (!string.IsNullOrEmpty(doctorIdClaim) || !string.IsNullOrEmpty(clinicIdClaim))
        {
            var appointment = await _appointmentRepo.GetByIdAsync(dto.AppointmentId);
            var origAppointment = await _appointmentRepo.GetByIdAsync(entity.AppointmentId);

            if (appointment == null || origAppointment == null) return NotFound(new { message = "Appointment not found" });

            if (!string.IsNullOrEmpty(doctorIdClaim))
            {
                var isAllowed = !string.IsNullOrEmpty(appointment.ClinicId) && 
                                await _clinicRepo.IsDoctorAuthorizedForClinicAsync(doctorIdClaim, appointment.ClinicId) &&
                                !string.IsNullOrEmpty(origAppointment.ClinicId) && 
                                await _clinicRepo.IsDoctorAuthorizedForClinicAsync(doctorIdClaim, origAppointment.ClinicId);
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

    /// <summary>
    /// BR-RX-02: Finalizes and digitally signs a prescription, permanently locking it.
    /// </summary>
    [HttpPost("{id}/finalize")]
    public async Task<IActionResult> FinalizePrescription(string id, [FromBody] FinalizePrescriptionDto? finalizeDto)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound(new { message = "Prescription not found" });

        if (entity.IsFinalized || entity.Status == "finalized")
        {
            return BadRequest(new { message = "Prescription is already finalized and digitally locked." });
        }

        if (entity.Status == "superseded")
        {
            return BadRequest(new { message = "Cannot finalize a prescription that has already been superseded." });
        }

        var rawDoctorName = finalizeDto?.DoctorName 
            ?? User.GetUserName() 
            ?? User.Identity?.Name 
            ?? "Attending Physician";
        var docFormatted = rawDoctorName.StartsWith("Dr.", StringComparison.OrdinalIgnoreCase) ? rawDoctorName : $"Dr. {rawDoctorName}";

        entity.IsFinalized = true;
        entity.Status = "finalized";
        entity.FinalizedAt = DateTime.UtcNow.ToString("o");
        entity.DigitalSignature = $"Digitally Signed by {docFormatted} on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC (Verified)";

        await _repo.UpdateAsync(entity);
        return Ok(new { 
            message = "Prescription finalized and digitally signed successfully.", 
            data = MapToDto(entity) 
        });
    }

    /// <summary>
    /// BR-RX-02: Issues a superseding prescription when medication change is needed,
    /// archiving the original with a mandatory recorded clinical justification.
    /// </summary>
    [HttpPost("{id}/supersede")]
    public async Task<IActionResult> SupersedePrescription(string id, [FromBody] SupersedePrescriptionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            return BadRequest(new { message = "A clinical justification reason is required to supersede a finalized prescription." });
        }

        if (dto.NewMedications == null || dto.NewMedications.Count == 0)
        {
            return BadRequest(new { message = "At least one medication is required for the superseding prescription." });
        }

        var original = await _repo.GetByIdAsync(id);
        if (original == null) return NotFound(new { message = "Original prescription not found" });

        if (original.Status == "superseded")
        {
            return BadRequest(new { message = "This prescription has already been superseded by a subsequent revision." });
        }

        var doctorName = User.GetUserName() ?? User.Identity?.Name ?? "Attending Physician";
        var doctorId = User.GetDoctorId() ?? User.GetUserId() ?? original.DoctorId;

        var newRxId = Guid.NewGuid().ToString();
        var origShortId = original.Id.Length >= 8 ? original.Id.Substring(0, 8) : original.Id;

        // 1. Create the new superseding prescription (automatically finalized with audit trail)
        var newRx = new Prescription
        {
            Id = newRxId,
            AppointmentId = original.AppointmentId,
            PatientId = original.PatientId,
            DoctorId = doctorId,
            Date = DateTime.UtcNow.ToString("o"),
            Notes = string.IsNullOrWhiteSpace(dto.Notes)
                ? $"[SUPERSEDING PRESCRIPTION - BR-RX-02]\nReplaces Rx #{origShortId}\nClinical Reason: {dto.Reason.Trim()}"
                : $"{dto.Notes.Trim()}\n\n[SUPERSEDING PRESCRIPTION - BR-RX-02]\nReplaces Rx #{origShortId}\nClinical Reason: {dto.Reason.Trim()}",
            IsFinalized = true,
            Status = "finalized",
            FinalizedAt = DateTime.UtcNow.ToString("o"),
            DigitalSignature = $"Digitally Signed by Dr. {doctorName} on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC (Verified Revision)",
            SupersedesPrescriptionId = original.Id,
            Medications = dto.NewMedications.Select(m => new MedicationItem
            {
                Name = m.Name, Dosage = m.Dosage, Frequency = m.Frequency, Duration = m.Duration
            }).ToList()
        };

        // 2. Archive and link the original prescription
        original.Status = "superseded";
        original.SupersededById = newRxId;
        original.SupersedeReason = dto.Reason.Trim();

        await _repo.UpdateAsync(original);
        await _repo.AddAsync(newRx);

        return Ok(new
        {
            message = "Prescription superseded successfully. A new legally signed revision has been issued.",
            data = MapToDto(newRx),
            original = MapToDto(original)
        });
    }

    /// <summary>
    /// BR-RX-02: Deletion of finalized or archived prescriptions is strictly forbidden.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound(new { message = "Prescription not found" });

        if (entity.IsFinalized || entity.Status == "finalized" || entity.Status == "superseded")
        {
            return BadRequest(new
            {
                message = "BR-RX-02: Finalized and archived prescriptions cannot be deleted from the database. Medical audit records are permanent and immutable."
            });
        }

        await _repo.DeleteAsync(id);
        return Ok(new { message = "Draft prescription deleted successfully." });
    }

    private static PrescriptionDto MapToDto(Prescription p) => new()
    {
        Id = p.Id,
        AppointmentId = p.AppointmentId,
        PatientId = p.PatientId,
        DoctorId = p.DoctorId,
        Date = p.Date,
        Notes = p.Notes,
        IsFinalized = p.IsFinalized,
        Status = p.Status,
        FinalizedAt = p.FinalizedAt,
        DigitalSignature = p.DigitalSignature,
        SupersedesPrescriptionId = p.SupersedesPrescriptionId,
        SupersededById = p.SupersededById,
        SupersedeReason = p.SupersedeReason,
        Medications = p.Medications.Select(m => new MedicationItemDto
        {
            Name = m.Name, Dosage = m.Dosage, Frequency = m.Frequency, Duration = m.Duration
        }).ToList()
    };

    private static Prescription MapToEntity(PrescriptionDto dto) => new()
    {
        Id = dto.Id,
        AppointmentId = dto.AppointmentId,
        PatientId = dto.PatientId,
        DoctorId = dto.DoctorId,
        Date = dto.Date,
        Notes = dto.Notes,
        IsFinalized = dto.IsFinalized,
        Status = string.IsNullOrEmpty(dto.Status) ? (dto.IsFinalized ? "finalized" : "draft") : dto.Status,
        FinalizedAt = dto.FinalizedAt,
        DigitalSignature = dto.DigitalSignature,
        SupersedesPrescriptionId = dto.SupersedesPrescriptionId,
        SupersededById = dto.SupersededById,
        SupersedeReason = dto.SupersedeReason,
        Medications = dto.Medications.Select(m => new MedicationItem
        {
            Name = m.Name, Dosage = m.Dosage, Frequency = m.Frequency, Duration = m.Duration
        }).ToList()
    };
}
