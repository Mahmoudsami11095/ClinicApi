using Clinic.Application.Common;
using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/clinical-notes")]
[Authorize(Roles = "admin,doctor")]
public class ClinicalNotesController : ControllerBase
{
    private readonly IClinicalNoteRepository _noteRepo;
    private readonly IPatientRepository _patientRepo;
    private readonly IClinicRepository _clinicRepo;

    public ClinicalNotesController(
        IClinicalNoteRepository noteRepo,
        IPatientRepository patientRepo,
        IClinicRepository clinicRepo)
    {
        _noteRepo = noteRepo;
        _patientRepo = patientRepo;
        _clinicRepo = clinicRepo;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? patientId)
    {
        List<ClinicalNote> notes;
        if (!string.IsNullOrEmpty(patientId))
        {
            notes = await _noteRepo.GetByPatientIdAsync(patientId);
        }
        else
        {
            notes = await _noteRepo.GetAllAsync();
        }

        var doctorIdClaim = User.GetDoctorId();
        var clinicIdClaim = User.GetClinicId();

        if (!string.IsNullOrEmpty(doctorIdClaim))
        {
            var allowedClinicIds = await _clinicRepo.GetAllowedClinicIdsForDoctorAsync(doctorIdClaim);
            notes = notes.Where(n => string.IsNullOrEmpty(n.ClinicId) || allowedClinicIds.Contains(n.ClinicId)).ToList();
        }
        else if (!string.IsNullOrEmpty(clinicIdClaim))
        {
            notes = notes.Where(n => string.IsNullOrEmpty(n.ClinicId) || n.ClinicId == clinicIdClaim).ToList();
        }

        var dtos = notes.Select(MapToDto).ToList();
        return Ok(new { data = dtos });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var note = await _noteRepo.GetByIdAsync(id);
        if (note == null) return NotFound(new { message = "Clinical note not found" });

        var doctorIdClaim = User.GetDoctorId();
        var clinicIdClaim = User.GetClinicId();

        if (!string.IsNullOrEmpty(doctorIdClaim) && !string.IsNullOrEmpty(note.ClinicId))
        {
            var isAllowed = await _clinicRepo.IsDoctorAuthorizedForClinicAsync(doctorIdClaim, note.ClinicId);
            if (!isAllowed) return StatusCode(403, new { message = "You can only view clinical notes for your clinics" });
        }
        else if (!string.IsNullOrEmpty(clinicIdClaim) && !string.IsNullOrEmpty(note.ClinicId))
        {
            if (note.ClinicId != clinicIdClaim)
                return StatusCode(403, new { message = "You can only view clinical notes for your assigned clinic" });
        }

        return Ok(new { data = MapToDto(note) });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClinicalNoteDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Notes))
        {
            return BadRequest(new { message = "Clinical note content cannot be empty." });
        }

        if (string.IsNullOrWhiteSpace(dto.PatientId))
        {
            return BadRequest(new { message = "PatientId is required." });
        }

        var patient = await _patientRepo.GetByIdAsync(dto.PatientId);
        if (patient == null)
        {
            return NotFound(new { message = "Patient not found." });
        }

        var doctorIdClaim = User.GetDoctorId();
        var clinicIdClaim = User.GetClinicId();

        var effectiveClinicId = !string.IsNullOrEmpty(dto.ClinicId) ? dto.ClinicId : patient.ClinicId ?? clinicIdClaim;

        if (!string.IsNullOrEmpty(doctorIdClaim) && !string.IsNullOrEmpty(effectiveClinicId))
        {
            var isAllowed = await _clinicRepo.IsDoctorAuthorizedForClinicAsync(doctorIdClaim, effectiveClinicId);
            if (!isAllowed) return StatusCode(403, new { message = "You can only manage clinical notes for your clinics" });
        }
        else if (!string.IsNullOrEmpty(clinicIdClaim) && !string.IsNullOrEmpty(effectiveClinicId))
        {
            if (effectiveClinicId != clinicIdClaim)
                return StatusCode(403, new { message = "You can only manage clinical notes for your assigned clinic" });
        }

        var authorId = doctorIdClaim ?? User.GetUserId() ?? "system";
        var authorName = User.GetUserName() ?? User.Identity?.Name ?? "Doctor";

        var note = new ClinicalNote
        {
            Id = Guid.NewGuid().ToString(),
            PatientId = dto.PatientId,
            DoctorId = authorId,
            DoctorName = authorName,
            ClinicId = effectiveClinicId,
            Title = string.IsNullOrWhiteSpace(dto.Title) ? "Clinical Encounter Note" : dto.Title.Trim(),
            Category = string.IsNullOrWhiteSpace(dto.Category) ? "Consultation" : dto.Category.Trim(),
            Notes = dto.Notes.Trim(),
            CreatedAt = DateTime.UtcNow.ToString("o")
        };

        await _noteRepo.AddAsync(note);
        return Ok(new { message = "Clinical note recorded successfully.", data = MapToDto(note) });
    }

    /// <summary>
    /// BR-RX-03 / BR-MED-01: Medical Record Immutability (Amendment Trail)
    /// PUT does NOT overwrite original notes. It appends a permanent, timestamped amendment.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Amend(string id, [FromBody] AmendClinicalNoteDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.AmendedText))
        {
            return BadRequest(new { message = "Amended text / addendum cannot be empty." });
        }

        var note = await _noteRepo.GetByIdAsync(id);
        if (note == null)
        {
            return NotFound(new { message = "Clinical note not found." });
        }

        var doctorIdClaim = User.GetDoctorId();
        var clinicIdClaim = User.GetClinicId();

        if (!string.IsNullOrEmpty(doctorIdClaim) && !string.IsNullOrEmpty(note.ClinicId))
        {
            var isAllowed = await _clinicRepo.IsDoctorAuthorizedForClinicAsync(doctorIdClaim, note.ClinicId);
            if (!isAllowed) return StatusCode(403, new { message = "You can only amend clinical notes for your clinics" });
        }
        else if (!string.IsNullOrEmpty(clinicIdClaim) && !string.IsNullOrEmpty(note.ClinicId))
        {
            if (note.ClinicId != clinicIdClaim)
                return StatusCode(403, new { message = "You can only amend clinical notes for your assigned clinic" });
        }

        var authorId = doctorIdClaim ?? User.GetUserId() ?? "system";
        var authorName = User.GetUserName() ?? User.Identity?.Name ?? "Doctor";

        // Immutable pattern: Original note text is NEVER modified.
        // A new amendment is appended to the audit trail preserving author identity.
        var amendment = new ClinicalNoteAmendment
        {
            Id = Guid.NewGuid().ToString(),
            OriginalNoteId = note.Id,
            AmendedText = dto.AmendedText.Trim(),
            Reason = string.IsNullOrWhiteSpace(dto.Reason) ? null : dto.Reason.Trim(),
            AuthorId = authorId,
            AuthorName = authorName,
            Timestamp = DateTime.UtcNow.ToString("o")
        };

        note.Amendments.Add(amendment);
        await _noteRepo.UpdateAsync(note);

        return Ok(new { 
            message = "Amendment appended successfully to clinical audit trail.", 
            data = MapToDto(note) 
        });
    }

    /// <summary>
    /// BR-RX-03 / BR-MED-01: Clinical encounter notes cannot be deleted from the database.
    /// </summary>
    [HttpDelete("{id}")]
    public IActionResult Delete(string id)
    {
        return BadRequest(new
        {
            message = "BR-RX-03 / BR-MED-01: Clinical encounter notes cannot be deleted from the database. Medical records are permanent and immutable."
        });
    }

    private static ClinicalNoteDto MapToDto(ClinicalNote n) => new()
    {
        Id = n.Id,
        PatientId = n.PatientId,
        DoctorId = n.DoctorId,
        DoctorName = n.DoctorName,
        ClinicId = n.ClinicId,
        CreatedAt = n.CreatedAt,
        Title = n.Title,
        Category = n.Category,
        Notes = n.Notes,
        Amendments = (n.Amendments ?? new List<ClinicalNoteAmendment>())
            .OrderBy(a => a.Timestamp)
            .Select(a => new ClinicalNoteAmendmentDto
            {
                Id = a.Id,
                OriginalNoteId = a.OriginalNoteId,
                AmendedText = a.AmendedText,
                Reason = a.Reason,
                AuthorId = a.AuthorId,
                AuthorName = a.AuthorName,
                Timestamp = a.Timestamp
            }).ToList()
    };
}
