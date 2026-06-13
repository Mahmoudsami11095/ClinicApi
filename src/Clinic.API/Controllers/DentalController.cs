using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/dental")]
[Authorize]
public class DentalController : ControllerBase
{
    private readonly IDentalLogRepository _repo;

    public DentalController(IDentalLogRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var logs = await _repo.GetAllAsync();
        var dtos = logs.Select(MapToDto).ToList();
        return Ok(new { data = dtos });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DentalLogDto dto)
    {
        var entity = new DentalLog
        {
            Id = string.IsNullOrEmpty(dto.Id) ? Guid.NewGuid().ToString() : dto.Id,
            PatientId = dto.PatientId,
            ToothNumber = dto.ToothNumber,
            DoctorId = dto.DoctorId,
            DoctorName = dto.DoctorName,
            Date = dto.Date,
            Status = JsonSerializer.Serialize(dto.Status),
            PainLevel = dto.PainLevel,
            PainDetails = dto.PainDetails,
            Treatment = dto.Treatment,
            Medication = dto.Medication,
            IsPlanned = dto.IsPlanned
        };
        await _repo.AddAsync(entity);
        return Ok(new { message = "Success", data = dto });
    }

    private static DentalLogDto MapToDto(DentalLog d)
    {
        List<string> statusList;
        try { statusList = JsonSerializer.Deserialize<List<string>>(d.Status) ?? new(); }
        catch { statusList = new List<string> { d.Status }; }

        return new DentalLogDto
        {
            Id = d.Id, PatientId = d.PatientId, ToothNumber = d.ToothNumber,
            DoctorId = d.DoctorId, DoctorName = d.DoctorName, Date = d.Date,
            Status = statusList, PainLevel = d.PainLevel,
            PainDetails = d.PainDetails, Treatment = d.Treatment,
            Medication = d.Medication, IsPlanned = d.IsPlanned
        };
    }
}
