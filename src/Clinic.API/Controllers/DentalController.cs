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
    private readonly IMaterialRepository _materialRepo;
    private readonly IClinicRepository _clinicRepo;
    private readonly IPatientRepository _patientRepo;

    public DentalController(IDentalLogRepository repo, IMaterialRepository materialRepo, IClinicRepository clinicRepo, IPatientRepository patientRepo)
    {
        _repo = repo;
        _materialRepo = materialRepo;
        _clinicRepo = clinicRepo;
        _patientRepo = patientRepo;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var logs = await _repo.GetAllAsync();
        var doctorIdClaim = User.FindFirst("doctorId")?.Value;
        var clinicIdClaim = User.FindFirst("clinicId")?.Value;

        if (!string.IsNullOrEmpty(doctorIdClaim) || !string.IsNullOrEmpty(clinicIdClaim))
        {
            if (!string.IsNullOrEmpty(doctorIdClaim))
            {
                var clinics = await _clinicRepo.GetAllAsync();
                var allowedClinicIds = clinics
                    .Where(c => c.CreatorDoctorId == doctorIdClaim || 
                                c.DoctorClinics.Any(dc => dc.DoctorId == doctorIdClaim && dc.Status == "Accepted"))
                    .Select(c => c.Id)
                    .ToList();
                logs = logs.Where(l => allowedClinicIds.Contains(l.ClinicId ?? "")).ToList();
            }
            else if (!string.IsNullOrEmpty(clinicIdClaim))
            {
                logs = logs.Where(l => l.ClinicId == clinicIdClaim).ToList();
            }
        }
        var dtos = logs.Select(MapToDto).ToList();
        return Ok(new { data = dtos });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DentalLogDto dto)
    {
        var doctorIdClaim = User.FindFirst("doctorId")?.Value;
        var clinicIdClaim = User.FindFirst("clinicId")?.Value;

        if (string.IsNullOrEmpty(dto.ClinicId) && !string.IsNullOrEmpty(dto.PatientId))
        {
            var patient = await _patientRepo.GetByIdAsync(dto.PatientId);
            if (patient != null)
            {
                dto.ClinicId = patient.ClinicId;
            }
        }

        if (!string.IsNullOrEmpty(doctorIdClaim) || !string.IsNullOrEmpty(clinicIdClaim))
        {
            if (!string.IsNullOrEmpty(doctorIdClaim))
            {
                var clinics = await _clinicRepo.GetAllAsync();
                var isAllowed = clinics.Any(c => c.Id == dto.ClinicId && 
                    (c.CreatorDoctorId == doctorIdClaim || 
                     c.DoctorClinics.Any(dc => dc.DoctorId == doctorIdClaim && dc.Status == "Accepted")));
                if (!isAllowed) return StatusCode(403, new { message = "You can only manage dental logs for your clinics" });
            }
            else if (!string.IsNullOrEmpty(clinicIdClaim))
            {
                if (dto.ClinicId != clinicIdClaim)
                    return StatusCode(403, new { message = "You can only manage dental logs for your assigned clinic" });
            }
        }
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
            IsPlanned = dto.IsPlanned,
            ConsumedMaterials = JsonSerializer.Serialize(dto.ConsumedMaterials),
            ClinicId = dto.ClinicId
        };
        await _repo.AddAsync(entity);

        // Deduct materials from inventory
        if (dto.ConsumedMaterials != null && dto.ConsumedMaterials.Any())
        {
            foreach (var cm in dto.ConsumedMaterials)
            {
                var material = await _materialRepo.GetByIdAsync(cm.MaterialId);
                if (material != null)
                {
                    material.Quantity -= cm.Quantity;
                    if (material.Quantity < 0) material.Quantity = 0;
                    await _materialRepo.UpdateAsync(material);
                }
            }
        }

        return Ok(new { message = "Success", data = dto });
    }

    private static DentalLogDto MapToDto(DentalLog d)
    {
        List<string> statusList;
        try { statusList = JsonSerializer.Deserialize<List<string>>(d.Status) ?? new(); }
        catch { statusList = new List<string> { d.Status }; }

        List<ConsumedMaterialDto> materialsList;
        try { materialsList = JsonSerializer.Deserialize<List<ConsumedMaterialDto>>(d.ConsumedMaterials) ?? new(); }
        catch { materialsList = new(); }

        return new DentalLogDto
        {
            Id = d.Id, PatientId = d.PatientId, ToothNumber = d.ToothNumber,
            DoctorId = d.DoctorId, DoctorName = d.DoctorName, Date = d.Date,
            Status = statusList, PainLevel = d.PainLevel,
            PainDetails = d.PainDetails, Treatment = d.Treatment,
            Medication = d.Medication, IsPlanned = d.IsPlanned,
            ConsumedMaterials = materialsList, ClinicId = d.ClinicId
        };
    }
}
