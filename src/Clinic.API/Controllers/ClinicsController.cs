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
    private readonly IDoctorRepository _doctorRepo;

    public ClinicsController(IClinicRepository repo, IDoctorRepository doctorRepo)
    {
        _repo = repo;
        _doctorRepo = doctorRepo;
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
                Status = status
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
            CreatorDoctorId = doctorIdClaim
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
            Status = "Accepted"
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
        await _repo.UpdateAsync(entity);

        var result = new ClinicDto { Id = entity.Id, Name = entity.Name, Address = entity.Address, Phone = entity.Phone, CreatorDoctorId = entity.CreatorDoctorId };
        return Ok(new { message = "Success", data = result });
    }
}

public class RespondAssignmentRequest
{
    public string Status { get; set; } = string.Empty;
}
