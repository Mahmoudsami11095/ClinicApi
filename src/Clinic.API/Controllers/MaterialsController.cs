using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/materials")]
[Authorize]
public class MaterialsController : ControllerBase
{
    private readonly IMaterialRepository _repo;
    private readonly IMaterialAlertService? _alertService;
    private readonly IMaterialSeedingService? _seedingService;
    private readonly ClinicDbContext? _context;

    public MaterialsController(
        IMaterialRepository repo, 
        IMaterialAlertService? alertService = null,
        IMaterialSeedingService? seedingService = null,
        ClinicDbContext? context = null)
    {
        _repo = repo;
        _alertService = alertService;
        _seedingService = seedingService;
        _context = context;
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

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? clinicId, [FromQuery] string? doctorId)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var doctorIdClaim = User.FindFirst("doctorId")?.Value;
        var clinicIdClaim = User.FindFirst("clinicId")?.Value;

        if (!string.IsNullOrEmpty(clinicIdClaim))
        {
            if (!string.IsNullOrEmpty(clinicId) && clinicId != "all" && clinicId != clinicIdClaim)
            {
                return StatusCode(403, new { message = "You can only view materials for your assigned clinic" });
            }
            clinicId = clinicIdClaim;
        }

        if (string.IsNullOrEmpty(doctorId) && !string.IsNullOrEmpty(doctorIdClaim) && role == "doctor")
        {
            doctorId = doctorIdClaim;
        }

        IEnumerable<Material> materials;

        if (!string.IsNullOrEmpty(clinicId) && clinicId != "all")
        {
            if (!string.IsNullOrEmpty(doctorId))
            {
                materials = await _repo.GetByDoctorAndClinicAsync(doctorId, clinicId);
            }
            else
            {
                var all = await _repo.GetAllAsync();
                materials = all.Where(m => m.ClinicId == clinicId);
            }
        }
        else if (!string.IsNullOrEmpty(doctorId))
        {
            materials = await _repo.GetByDoctorIdAsync(doctorId);
        }
        else
        {
            if (role == "assistant")
            {
                var assistantClinicIds = User.FindAll("clinicIds").Select(c => c.Value).ToList();
                var singleClinicId = User.FindFirst("clinicId")?.Value;
                if (!string.IsNullOrEmpty(singleClinicId) && !assistantClinicIds.Contains(singleClinicId))
                {
                    assistantClinicIds.Add(singleClinicId);
                }

                var all = await _repo.GetAllAsync();
                materials = assistantClinicIds.Any()
                    ? all.Where(m => assistantClinicIds.Contains(m.ClinicId ?? ""))
                    : all;
            }
            else
            {
                materials = await _repo.GetAllAsync();
            }
        }

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

        var doctorId = !string.IsNullOrEmpty(dto.DoctorId) ? dto.DoctorId : User.FindFirst("doctorId")?.Value;
        if (string.IsNullOrEmpty(doctorId) && _context != null)
        {
            var clinic = await _context.Clinics.Include(c => c.DoctorClinics).FirstOrDefaultAsync(c => c.Id == dto.ClinicId);
            doctorId = clinic?.CreatorDoctorId ?? clinic?.DoctorClinics.FirstOrDefault()?.DoctorId;
            if (string.IsNullOrEmpty(doctorId))
            {
                var anyDoctor = await _context.Doctors.FirstOrDefaultAsync();
                doctorId = anyDoctor?.Id ?? "doc-default";
            }
        }
        if (string.IsNullOrEmpty(doctorId))
        {
            doctorId = "doc-default";
        }

        var material = new Material
        {
            Id = string.IsNullOrEmpty(dto.Id) ? Guid.NewGuid().ToString() : dto.Id,
            ClinicId = dto.ClinicId,
            DoctorId = doctorId,
            Name = dto.Name,
            Category = dto.Category,
            IsDefault = dto.IsDefault,
            Quantity = dto.Quantity,
            Unit = dto.Unit,
            MinStockAlert = dto.MinStockAlert > 0 ? dto.MinStockAlert : 5,
            ExpirationDate = dto.ExpirationDate,
            BatchNumber = dto.BatchNumber,
            SupplierName = dto.SupplierName,
            UnitCost = dto.UnitCost,
            PurchaseOrderRef = dto.PurchaseOrderRef,
            LastRestockedAt = DateTime.UtcNow.ToString("o")
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
        if (dto.Category != null) material.Category = dto.Category;
        material.Quantity = dto.Quantity;
        material.Unit = dto.Unit;
        material.ClinicId = dto.ClinicId;
        material.MinStockAlert = dto.MinStockAlert > 0 ? dto.MinStockAlert : 5;
        material.ExpirationDate = dto.ExpirationDate;
        material.BatchNumber = dto.BatchNumber;
        material.SupplierName = dto.SupplierName;
        material.UnitCost = dto.UnitCost;
        material.PurchaseOrderRef = dto.PurchaseOrderRef;

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

    /// <summary>
    /// REQ-INV-02: Records an inward stock shipment / supplier purchase order delivery.
    /// Increments inventory quantity, updates batch number, expiry date, supplier name, and unit cost.
    /// </summary>
    [HttpPost("{id}/inward-shipment")]
    public async Task<IActionResult> ReceiveInwardShipment(string id, [FromBody] InwardShipmentRequest request)
    {
        var material = await _repo.GetByIdAsync(id);
        if (material == null) return NotFound(new { message = "Material not found" });

        if (request.QuantityReceived <= 0)
        {
            return BadRequest(new { message = "Quantity received must be greater than zero." });
        }

        var clinicIdClaim = User.FindFirst("clinicId")?.Value;
        if (!string.IsNullOrEmpty(clinicIdClaim) && material.ClinicId != clinicIdClaim)
        {
            return StatusCode(403, new { message = "You can only manage materials for your assigned clinic" });
        }

        material.Quantity += request.QuantityReceived;
        if (!string.IsNullOrWhiteSpace(request.SupplierName)) material.SupplierName = request.SupplierName;
        if (!string.IsNullOrWhiteSpace(request.PurchaseOrderRef)) material.PurchaseOrderRef = request.PurchaseOrderRef;
        if (!string.IsNullOrWhiteSpace(request.BatchNumber)) material.BatchNumber = request.BatchNumber;
        if (request.ExpirationDate.HasValue) material.ExpirationDate = request.ExpirationDate;
        if (request.UnitCost.HasValue) material.UnitCost = request.UnitCost;
        material.LastRestockedAt = DateTime.UtcNow.ToString("o");

        await _repo.UpdateAsync(material);

        if (_alertService != null)
        {
            await _alertService.CheckAndTriggerLowStockAlertAsync(material);
        }

        return Ok(new
        {
            message = $"Successfully received inward shipment of {request.QuantityReceived} {material.Unit ?? "units"}.",
            data = MapToDto(material)
        });
    }

    [HttpPost("seed-defaults")]
    public async Task<IActionResult> SeedDefaults([FromQuery] string clinicId)
    {
        if (string.IsNullOrWhiteSpace(clinicId) || clinicId == "all")
            return BadRequest(new { message = "A specific clinicId is required to seed default materials." });

        var doctorIdClaim = User.FindFirst("doctorId")?.Value;
        var clinicIdClaim = User.FindFirst("clinicId")?.Value;

        if (!string.IsNullOrEmpty(clinicIdClaim) && clinicIdClaim != clinicId)
            return StatusCode(403, new { message = "You can only seed materials for your assigned clinic." });

        var targetDoctorId = doctorIdClaim;
        if (string.IsNullOrEmpty(targetDoctorId) && _context != null)
        {
            var clinic = await _context.Clinics.Include(c => c.DoctorClinics).FirstOrDefaultAsync(c => c.Id == clinicId);
            targetDoctorId = clinic?.CreatorDoctorId ?? clinic?.DoctorClinics.FirstOrDefault()?.DoctorId;
            if (string.IsNullOrEmpty(targetDoctorId))
            {
                var anyDoctor = await _context.Doctors.FirstOrDefaultAsync();
                targetDoctorId = anyDoctor?.Id ?? "doc-default";
            }
        }

        if (string.IsNullOrEmpty(targetDoctorId))
        {
            targetDoctorId = "doc-default";
        }

        if (_seedingService != null)
        {
            await _seedingService.SeedDefaultMaterialsAsync(clinicId, targetDoctorId);
        }

        var all = await _repo.GetAllAsync();
        var materials = all.Where(m => m.ClinicId == clinicId);
        return Ok(new { message = "Default materials seeded successfully.", data = materials.Select(MapToDto) });
    }

    private static MaterialDto MapToDto(Material m) => new()
    {
        Id = m.Id,
        ClinicId = m.ClinicId,
        DoctorId = m.DoctorId,
        Name = m.Name,
        Category = m.Category,
        IsDefault = m.IsDefault,
        Quantity = m.Quantity,
        Unit = m.Unit,
        MinStockAlert = m.MinStockAlert,
        ExpirationDate = m.ExpirationDate,
        BatchNumber = m.BatchNumber,
        SupplierName = m.SupplierName,
        UnitCost = m.UnitCost,
        LastRestockedAt = m.LastRestockedAt,
        PurchaseOrderRef = m.PurchaseOrderRef
    };
}

public class InwardShipmentRequest
{
    public int QuantityReceived { get; set; }
    public string? SupplierName { get; set; }
    public string? PurchaseOrderRef { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public decimal? UnitCost { get; set; }
}
