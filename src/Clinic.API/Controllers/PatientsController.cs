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

    public PatientsController(IPatientRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var patients = await _repo.GetAllAsync();
        var dtos = patients.Select(p => new PatientDto
        {
            Id = p.Id, FirstName = p.FirstName, LastName = p.LastName,
            Gender = p.Gender, DateOfBirth = p.DateOfBirth,
            ContactNumber = p.ContactNumber, Email = p.Email,
            BloodGroup = p.BloodGroup, Address = p.Address,
            RegistrationDate = p.RegistrationDate, ClinicId = p.ClinicId
        }).ToList();
        return Ok(new { data = dtos });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PatientDto dto)
    {
        var entity = new Patient
        {
            Id = string.IsNullOrEmpty(dto.Id) ? Guid.NewGuid().ToString() : dto.Id,
            FirstName = dto.FirstName, LastName = dto.LastName,
            Gender = dto.Gender, DateOfBirth = dto.DateOfBirth,
            ContactNumber = dto.ContactNumber, Email = dto.Email,
            BloodGroup = dto.BloodGroup, Address = dto.Address,
            RegistrationDate = dto.RegistrationDate, ClinicId = dto.ClinicId
        };
        await _repo.AddAsync(entity);
        return Ok(new { message = "Success" });
    }
}
