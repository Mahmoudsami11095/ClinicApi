using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Clinic.Domain.Enums;
using Clinic.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/clinics")]
[Authorize]
public class ClinicsController : ControllerBase
{
    private readonly IClinicRepository _repo;
    private readonly IDoctorRepository _doctorRepo;
    private readonly ClinicDbContext _context;

    public ClinicsController(IClinicRepository repo, IDoctorRepository doctorRepo, ClinicDbContext context)
    {
        _repo = repo;
        _doctorRepo = doctorRepo;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var clinics = await _repo.GetAllAsync();
        var doctorIdClaim = User.FindFirst("doctorId")?.Value;

        if (!string.IsNullOrEmpty(doctorIdClaim))
        {
            // Filter clinics to show only those created by the doctor or assigned to them (either Pending or Accepted)
            clinics = clinics.Where(c => c.CreatorDoctorId == doctorIdClaim || 
                                         c.DoctorClinics.Any(dc => dc.DoctorId == doctorIdClaim)).ToList();
        }

        var dtos = clinics.Select(c => {
            var status = "Accepted";
            if (!string.IsNullOrEmpty(doctorIdClaim))
            {
                var rel = c.DoctorClinics.FirstOrDefault(dc => dc.DoctorId == doctorIdClaim);
                status = rel?.Status ?? (c.CreatorDoctorId == doctorIdClaim ? "Accepted" : "None");
            }
            return new ClinicDto
            {
                Id = c.Id, 
                Name = c.Name, 
                Address = c.Address, 
                Phone = c.Phone,
                CreatorDoctorId = c.CreatorDoctorId,
                Status = status,
                AvailabilityHours = c.AvailabilityHours,
                AvailabilityDays = c.AvailabilityDays
            };
        }).ToList();

        return Ok(new { data = dtos });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ClinicDto dto)
    {
        var doctorIdClaim = User.FindFirst("doctorId")?.Value;
        if (!string.IsNullOrEmpty(doctorIdClaim))
        {
            var doctor = await _doctorRepo.GetByIdAsync(doctorIdClaim);
            if (doctor == null)
            {
                return Unauthorized(new { message = "Doctor profile not found. Please log in again." });
            }
        }

        var entity = new ClinicEntity
        {
            Id = string.IsNullOrEmpty(dto.Id) ? Guid.NewGuid().ToString() : dto.Id,
            Name = dto.Name, 
            Address = dto.Address, 
            Phone = dto.Phone,
            CreatorDoctorId = doctorIdClaim,
            AvailabilityHours = dto.AvailabilityHours,
            AvailabilityDays = dto.AvailabilityDays
        };
        await _repo.AddAsync(entity);

        // Auto-assign the creating doctor if applicable
        if (!string.IsNullOrEmpty(doctorIdClaim))
        {
            await _doctorRepo.AssignToClinicAsync(doctorIdClaim, entity.Id);
        }

        var result = new ClinicDto 
        { 
            Id = entity.Id, 
            Name = entity.Name, 
            Address = entity.Address, 
            Phone = entity.Phone,
            CreatorDoctorId = entity.CreatorDoctorId,
            Status = "Accepted",
            AvailabilityHours = entity.AvailabilityHours,
            AvailabilityDays = entity.AvailabilityDays
        };
        return Ok(new { message = "Success", data = result });
    }

    [HttpPost("{id}/assign-doctors")]
    public async Task<IActionResult> AssignDoctors(string id, [FromBody] List<string> doctorIds)
    {
        var clinic = await _repo.GetByIdAsync(id);
        if (clinic == null) return NotFound(new { message = "Clinic not found" });

        var doctorIdClaim = User.FindFirst("doctorId")?.Value;
        // If doctor role, they must be the creator of the clinic
        if (!string.IsNullOrEmpty(doctorIdClaim) && clinic.CreatorDoctorId != doctorIdClaim)
        {
            return StatusCode(403, new { message = "Only the clinic creator can manage doctor assignments" });
        }

        if (doctorIds == null)
            doctorIds = new List<string>();

        await _doctorRepo.AssignDoctorsToClinicAsync(id, doctorIds);
        return Ok(new { message = "Doctors assigned successfully" });
    }

    [HttpPost("{id}/assign-doctors-by-emails")]
    public async Task<IActionResult> AssignDoctorsByEmails(string id, [FromBody] List<string> emails)
    {
        var clinic = await _repo.GetByIdAsync(id);
        if (clinic == null) return NotFound(new { message = "Clinic not found" });

        var doctorIdClaim = User.FindFirst("doctorId")?.Value;
        // If doctor role, they must be the creator of the clinic
        if (!string.IsNullOrEmpty(doctorIdClaim) && clinic.CreatorDoctorId != doctorIdClaim)
        {
            return StatusCode(403, new { message = "Only the clinic creator can manage doctor assignments" });
        }

        if (emails == null || !emails.Any())
            return BadRequest(new { message = "Email list cannot be empty" });

        var notFoundEmails = new List<string>();
        var assignedEmails = new List<string>();

        foreach (var email in emails)
        {
            var cleanEmail = email.Trim().ToLower();
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email.ToLower() == cleanEmail);
            if (doctor == null)
            {
                notFoundEmails.Add(email);
                continue;
            }

            var exists = await _context.DoctorClinics.AnyAsync(dc => dc.DoctorId == doctor.Id && dc.ClinicId == id);
            if (!exists)
            {
                await _context.DoctorClinics.AddAsync(new DoctorClinic
                {
                    DoctorId = doctor.Id,
                    ClinicId = id,
                    Status = "Pending"
                });
                assignedEmails.Add(email);
            }
        }

        await _context.SaveChangesAsync();

        if (notFoundEmails.Any())
        {
            return Ok(new { 
                message = $"Assigned {assignedEmails.Count} doctor(s). Note: Some emails were not found or already assigned.",
                assigned = assignedEmails,
                notFound = notFoundEmails
            });
        }

        return Ok(new { message = "Doctors assigned successfully", assigned = assignedEmails });
    }

    [HttpPost("{id}/assign-assistant")]
    public async Task<IActionResult> AssignAssistant(string id, [FromBody] AssignAssistantRequest request)
    {
        var clinic = await _repo.GetByIdAsync(id);
        if (clinic == null) return NotFound(new { message = "Clinic not found" });

        var doctorIdClaim = User.FindFirst("doctorId")?.Value;
        // Verify user has permission: only the clinic creator, assigned doctor or admin can manage it
        if (!string.IsNullOrEmpty(doctorIdClaim))
        {
            var isCreator = clinic.CreatorDoctorId == doctorIdClaim;
            var isAssigned = await _context.DoctorClinics.AnyAsync(dc => dc.DoctorId == doctorIdClaim && dc.ClinicId == id && dc.Status == "Accepted");
            if (!isCreator && !isAssigned)
            {
                return StatusCode(403, new { message = "You do not have permission to manage this clinic" });
            }
        }

        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "Assistant email is required" });

        var cleanEmail = request.Email.Trim().ToLower();
        var assistantUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == cleanEmail);
        
        if (assistantUser == null)
            return NotFound(new { message = $"No user found with email {request.Email}" });

        if (assistantUser.Role != UserRole.Assistant)
            return BadRequest(new { message = $"User with email {request.Email} is not an Assistant" });

        // Update assistant's clinic and supervising doctor
        assistantUser.ClinicId = id;
        if (!string.IsNullOrEmpty(doctorIdClaim))
        {
            assistantUser.DoctorId = doctorIdClaim;
        }

        _context.Users.Update(assistantUser);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Assistant assigned successfully", assistant = assistantUser.Name });
    }

    [HttpPost("{id}/respond-assignment")]
    public async Task<IActionResult> RespondAssignment(string id, [FromBody] RespondAssignmentRequest request)
    {
        var doctorIdClaim = User.FindFirst("doctorId")?.Value;
        if (string.IsNullOrEmpty(doctorIdClaim))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Status))
            return BadRequest(new { message = "Status (Accepted/Rejected) is required" });

        await _doctorRepo.RespondToAssignmentAsync(doctorIdClaim, id, request.Status);
        return Ok(new { message = "Response registered successfully" });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] ClinicDto dto)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound(new { message = "Not found" });

        var doctorIdClaim = User.FindFirst("doctorId")?.Value;
        // Only the creator can update the clinic details if doctor is logged in
        if (!string.IsNullOrEmpty(doctorIdClaim) && entity.CreatorDoctorId != doctorIdClaim)
        {
            return StatusCode(403, new { message = "Only the clinic creator can edit details" });
        }

        entity.Name = dto.Name;
        entity.Address = dto.Address;
        entity.Phone = dto.Phone;
        entity.AvailabilityHours = dto.AvailabilityHours;
        entity.AvailabilityDays = dto.AvailabilityDays;
        await _repo.UpdateAsync(entity);

        var result = new ClinicDto { Id = entity.Id, Name = entity.Name, Address = entity.Address, Phone = entity.Phone, CreatorDoctorId = entity.CreatorDoctorId, AvailabilityHours = entity.AvailabilityHours, AvailabilityDays = entity.AvailabilityDays };
        return Ok(new { message = "Success", data = result });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound(new { message = "Clinic not found" });

        var doctorIdClaim = User.FindFirst("doctorId")?.Value;
        var isAdmin = User.IsInRole("admin");
        if (!isAdmin && !string.IsNullOrEmpty(doctorIdClaim) && entity.CreatorDoctorId != doctorIdClaim)
        {
            return StatusCode(403, new { message = "Only the clinic creator or an admin can delete the clinic" });
        }

        await _repo.DeleteAsync(id);
        return Ok(new { message = "Clinic deleted successfully" });
    }
}

public class RespondAssignmentRequest
{
    public string Status { get; set; } = string.Empty;
}

public class AssignAssistantRequest
{
    public string Email { get; set; } = string.Empty;
}
