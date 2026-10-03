using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/materials")]
[Authorize]
public class MaterialsController : ControllerBase
{
    private readonly IMaterialRepository _repo;
    private readonly IMaterialAlertService? _alertService;

    public MaterialsController(IMaterialRepository repo, IMaterialAlertService? alertService = null)
    {
        _repo = repo;
        _alertService = alertService;
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock([FromQuery] string? clinicId, [FromQuery] string? doctorId)
    {
        var clinicIdClaim = User.FindFirst("clinicId")?.Value;
        if (!string.IsNullOrEmpty(clinicIdClaim))
        {
            clinicId = clinicIdClaim;
        }

        var materials = await _repo.GetLowStockAsync(clinicId, doctorId);
        var dtos = materials.Select(MapToDto);
        return Ok(new { data = dtos });
    }

    [HttpGet("expired")]
    public async Task<IActionResult> GetExpired([FromQuery] string? clinicId, [FromQuery] string? doctorId)
    {
        var clinicIdClaim = User.FindFirst("clinicId")?.Value;
        if (!string.IsNullOrEmpty(clinicIdClaim))
        {
            clinicId = clinicIdClaim;
        }

        var materials = await _repo.GetExpiredAsync(clinicId, doctorId);
        var dtos = materials.Select(MapToDto);
        return Ok(new { data = dtos });
    }

    [HttpGet("doctor/{doctorId}")]
    public async Task<IActionResult> GetByDoctor(string doctorId, [FromQuery] string? clinicId)
    {
        var clinicIdClaim = User.FindFirst("clinicId")?.Value;
        if (!string.IsNullOrEmpty(clinicIdClaim))
        {
            if (!string.IsNullOrEmpty(clinicId) && clinicId != "all" && clinicId != clinicIdClaim)
            {
                return StatusCode(403, new { message = "You can only view materials for your assigned clinic" });
            }
            clinicId = clinicIdClaim;
        }

        IEnumerable<Material> materials;
        if (!string.IsNullOrEmpty(clinicId) && clinicId != "all")
        {
            materials = await _repo.GetByDoctorAndClinicAsync(doctorId, clinicId);
        }
        else
        {
            materials = await _repo.GetByDoctorIdAsync(doctorId);
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (role == "assistant")
            {
                var assistantClinicIds = User.FindAll("clinicIds").Select(c => c.Value).ToList();
                var singleClinicId = User.FindFirst("clinicId")?.Value;
                if (!string.IsNullOrEmpty(singleClinicId) && !assistantClinicIds.Contains(singleClinicId))
                {
                    assistantClinicIds.Add(singleClinicId);
                }

                if (assistantClinicIds.Any())
                {
                    materials = materials.Where(m => assistantClinicIds.Contains(m.ClinicId ?? ""));
                }
            }
        }

        var dtos = materials.Select(MapToDto);
        return Ok(new { data = dtos });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MaterialDto dto)
    {
        var clinicIdClaim = User.FindFirst("clinicId")?.Value;
        if (!string.IsNullOrEmpty(clinicIdClaim))
        {
            if (dto.ClinicId != clinicIdClaim)
                return StatusCode(403, new { message = "You can only manage materials for your assigned clinic" });
        }

        var material = new Material
        {
            Id = string.IsNullOrEmpty(dto.Id) ? Guid.NewGuid().ToString() : dto.Id,
            ClinicId = dto.ClinicId,
            DoctorId = dto.DoctorId,
            Name = dto.Name,
            Quantity = dto.Quantity,
            Unit = dto.Unit,
            MinStockAlert = dto.MinStockAlert > 0 ? dto.MinStockAlert : 5,
            ExpirationDate = dto.ExpirationDate,
            BatchNumber = dto.BatchNumber
        };
        await _repo.AddAsync(material);
        dto.Id = material.Id;

        if (_alertService != null)
        {
            await _alertService.CheckAndTriggerLowStockAlertAsync(material);
        }

        return Ok(new { message = "Material added successfully", data = MapToDto(material) });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] MaterialDto dto)
    {
        var material = await _repo.GetByIdAsync(id);
        if (material == null) return NotFound(new { message = "Material not found" });

        var clinicIdClaim = User.FindFirst("clinicId")?.Value;
        if (!string.IsNullOrEmpty(clinicIdClaim))
        {
            if (dto.ClinicId != clinicIdClaim || material.ClinicId != clinicIdClaim)
                return StatusCode(403, new { message = "You can only manage materials for your assigned clinic" });
        }

        material.Name = dto.Name;
        material.Quantity = dto.Quantity;
        material.Unit = dto.Unit;
        material.ClinicId = dto.ClinicId;
        material.MinStockAlert = dto.MinStockAlert > 0 ? dto.MinStockAlert : 5;
        material.ExpirationDate = dto.ExpirationDate;
        material.BatchNumber = dto.BatchNumber;

        await _repo.UpdateAsync(material);

        if (_alertService != null)
        {
            await _alertService.CheckAndTriggerLowStockAlertAsync(material);
        }

        return Ok(new { message = "Material updated successfully", data = MapToDto(material) });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var material = await _repo.GetByIdAsync(id);
        if (material == null) return NotFound(new { message = "Material not found" });

        var clinicIdClaim = User.FindFirst("clinicId")?.Value;
        if (!string.IsNullOrEmpty(clinicIdClaim))
        {
            if (material.ClinicId != clinicIdClaim)
                return StatusCode(403, new { message = "You can only manage materials for your assigned clinic" });
        }

        await _repo.DeleteAsync(id);
        return Ok(new { message = "Material deleted successfully" });
    }

    private static MaterialDto MapToDto(Material m) => new()
    {
        Id = m.Id,
        ClinicId = m.ClinicId,
        DoctorId = m.DoctorId,
        Name = m.Name,
        Quantity = m.Quantity,
        Unit = m.Unit,
        MinStockAlert = m.MinStockAlert,
        ExpirationDate = m.ExpirationDate,
        BatchNumber = m.BatchNumber
    };
}
