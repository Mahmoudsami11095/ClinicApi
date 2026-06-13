using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/patients")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly IPatientRepository _repo;
    private readonly IClinicRepository _clinicRepo;

    public PatientsController(IPatientRepository repo, IClinicRepository clinicRepo)
    {
        _repo = repo;
        _clinicRepo = clinicRepo;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var patients = await _repo.GetAllAsync();
        var doctorIdClaim = User.FindFirst("doctorId")?.Value;
        if (!string.IsNullOrEmpty(doctorIdClaim))
        {
            var clinics = await _clinicRepo.GetAllAsync();
            var allowedClinicIds = clinics
                .Where(c => c.CreatorDoctorId == doctorIdClaim || 
                            c.DoctorClinics.Any(dc => dc.DoctorId == doctorIdClaim && dc.Status == "Accepted"))
                .Select(c => c.Id)
                .ToList();
            patients = patients.Where(p => allowedClinicIds.Contains(p.ClinicId ?? "")).ToList();
        }

        var dtos = patients.Select(p => new PatientDto
        {
            Id = p.Id, FirstName = p.FirstName, LastName = p.LastName,
            Gender = p.Gender, DateOfBirth = p.DateOfBirth,
            ContactNumber = p.ContactNumber, Email = p.Email,
            BloodGroup = p.BloodGroup, Address = p.Address,
            RegistrationDate = p.RegistrationDate, ClinicId = p.ClinicId,
            Allergies = p.Allergies, ChronicDiseases = p.ChronicDiseases, PastIllnesses = p.PastIllnesses
        }).ToList();
        return Ok(new { data = dtos });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PatientDto dto)
    {
        var doctorIdClaim = User.FindFirst("doctorId")?.Value;
        if (!string.IsNullOrEmpty(doctorIdClaim))
        {
            var clinics = await _clinicRepo.GetAllAsync();
            var isAllowed = clinics.Any(c => c.Id == dto.ClinicId && 
                (c.CreatorDoctorId == doctorIdClaim || 
                 c.DoctorClinics.Any(dc => dc.DoctorId == doctorIdClaim && dc.Status == "Accepted")));
            if (!isAllowed)
                return StatusCode(403, new { message = "You can only manage patients for your clinics" });
        }

        var entity = new Patient
        {
            Id = string.IsNullOrEmpty(dto.Id) ? Guid.NewGuid().ToString() : dto.Id,
            FirstName = dto.FirstName, LastName = dto.LastName,
            Gender = dto.Gender, DateOfBirth = dto.DateOfBirth,
            ContactNumber = dto.ContactNumber, Email = dto.Email,
            BloodGroup = dto.BloodGroup, Address = dto.Address,
            RegistrationDate = dto.RegistrationDate, ClinicId = dto.ClinicId,
            Allergies = dto.Allergies, ChronicDiseases = dto.ChronicDiseases, PastIllnesses = dto.PastIllnesses
        };
        await _repo.AddAsync(entity);
        return Ok(new { message = "Success" });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] PatientDto dto)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        var doctorIdClaim = User.FindFirst("doctorId")?.Value;
        if (!string.IsNullOrEmpty(doctorIdClaim))
        {
            var clinics = await _clinicRepo.GetAllAsync();
            var isAllowed = clinics.Any(c => c.Id == dto.ClinicId && 
                (c.CreatorDoctorId == doctorIdClaim || 
                 c.DoctorClinics.Any(dc => dc.DoctorId == doctorIdClaim && dc.Status == "Accepted")));
            if (!isAllowed)
                return StatusCode(403, new { message = "You can only manage patients for your clinics" });
        }

        existing.FirstName = dto.FirstName;
        existing.LastName = dto.LastName;
        existing.Gender = dto.Gender;
        existing.DateOfBirth = dto.DateOfBirth;
        existing.ContactNumber = dto.ContactNumber;
        existing.Email = dto.Email;
        existing.BloodGroup = dto.BloodGroup;
        existing.Address = dto.Address;
        existing.ClinicId = dto.ClinicId;
        existing.Allergies = dto.Allergies;
        existing.ChronicDiseases = dto.ChronicDiseases;
        existing.PastIllnesses = dto.PastIllnesses;

        await _repo.UpdateAsync(existing);
        return Ok(new { message = "Success" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        var doctorIdClaim = User.FindFirst("doctorId")?.Value;
        if (!string.IsNullOrEmpty(doctorIdClaim))
        {
            var clinics = await _clinicRepo.GetAllAsync();
            var isAllowed = clinics.Any(c => c.Id == existing.ClinicId && 
                (c.CreatorDoctorId == doctorIdClaim || 
                 c.DoctorClinics.Any(dc => dc.DoctorId == doctorIdClaim && dc.Status == "Accepted")));
            if (!isAllowed)
                return StatusCode(403, new { message = "You can only manage patients for your clinics" });
        }

        await _repo.DeleteAsync(id);
        return Ok(new { message = "Success" });
    }
}

