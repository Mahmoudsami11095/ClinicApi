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
[Route("api/[controller]")]
[Authorize(Roles = "admin,doctor,assistant")]
public class EquipmentController : ControllerBase
{
    private readonly IEquipmentRepository _repo;
    private readonly ClinicDbContext? _context;

    public EquipmentController(IEquipmentRepository repo, ClinicDbContext? context = null)
    {
        _repo = repo;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetEquipment([FromQuery] string? clinicId)
    {
        var clinicIdClaim = User.FindFirst("clinicId")?.Value;
        if (!string.IsNullOrEmpty(clinicIdClaim))
        {
            if (!string.IsNullOrEmpty(clinicId) && clinicId != "all" && clinicId != clinicIdClaim)
            {
                return StatusCode(403, new { message = "You can only view equipment for your assigned clinic" });
            }
            clinicId = clinicIdClaim;
        }

        IEnumerable<Equipment> equipmentList;
        if (!string.IsNullOrEmpty(clinicId) && clinicId != "all")
        {
            equipmentList = await _repo.GetByClinicIdAsync(clinicId);
        }
        else
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (role == "assistant")
            {
                var assistantClinicIds = User.FindAll("clinicIds").Select(c => c.Value).ToList();
                var singleClinicId = User.FindFirst("clinicId")?.Value;
                if (!string.IsNullOrEmpty(singleClinicId) && !assistantClinicIds.Contains(singleClinicId))
                {
                    assistantClinicIds.Add(singleClinicId);
                }

                equipmentList = assistantClinicIds.Any()
                    ? await _repo.GetByClinicIdsAsync(assistantClinicIds)
                    : await _repo.GetAllAsync();
            }
            else
            {
                equipmentList = await _repo.GetAllAsync();
            }
        }

        var dtos = equipmentList.Select(MapToDto);
        return Ok(new { data = dtos });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var item = await _repo.GetByIdAsync(id);
        if (item == null) return NotFound(new { message = "Equipment not found" });

        var clinicIdClaim = User.FindFirst("clinicId")?.Value;
        if (!string.IsNullOrEmpty(clinicIdClaim) && item.ClinicId != clinicIdClaim)
        {
            return StatusCode(403, new { message = "You do not have access to this equipment" });
        }

        return Ok(new { data = MapToDto(item) });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EquipmentDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { message = "Equipment name is required." });
        }

        var clinicIdClaim = User.FindFirst("clinicId")?.Value;
        if (!string.IsNullOrEmpty(clinicIdClaim))
        {
            dto.ClinicId = clinicIdClaim;
        }

        if (string.IsNullOrEmpty(dto.ClinicId) || dto.ClinicId == "all")
        {
            return BadRequest(new { message = "A specific clinic must be selected." });
        }

        var doctorIdClaim = User.FindFirst("doctorId")?.Value;

        var equipment = new Equipment
        {
            Id = string.IsNullOrEmpty(dto.Id) ? Guid.NewGuid().ToString() : dto.Id,
            ClinicId = dto.ClinicId,
            DoctorId = !string.IsNullOrEmpty(dto.DoctorId) ? dto.DoctorId : doctorIdClaim,
            Name = dto.Name.Trim(),
            Category = string.IsNullOrWhiteSpace(dto.Category) ? "General" : dto.Category.Trim(),
            SerialNumber = dto.SerialNumber?.Trim(),
            ModelNumber = dto.ModelNumber?.Trim(),
            Manufacturer = dto.Manufacturer?.Trim(),
            RoomOrChair = dto.RoomOrChair?.Trim(),
            Status = string.IsNullOrWhiteSpace(dto.Status) ? "Operational" : dto.Status.Trim(),
            PurchaseCost = dto.PurchaseCost,
            PurchaseDate = dto.PurchaseDate,
            WarrantyExpiryDate = dto.WarrantyExpiryDate,
            LastMaintenanceDate = dto.LastMaintenanceDate,
            NextMaintenanceDate = dto.NextMaintenanceDate,
            MaintenanceNotes = dto.MaintenanceNotes,
            ServiceProvider = dto.ServiceProvider,
            ServiceContactPhone = dto.ServiceContactPhone,
            ImageUrl = dto.ImageUrl?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(equipment);
        return Ok(new { message = "Equipment registered successfully.", data = MapToDto(equipment) });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] EquipmentDto dto)
    {
        var item = await _repo.GetByIdAsync(id);
        if (item == null) return NotFound(new { message = "Equipment not found" });

        var clinicIdClaim = User.FindFirst("clinicId")?.Value;
        if (!string.IsNullOrEmpty(clinicIdClaim) && item.ClinicId != clinicIdClaim)
        {
            return StatusCode(403, new { message = "You can only manage equipment for your assigned clinic" });
        }

        item.Name = dto.Name.Trim();
        item.Category = string.IsNullOrWhiteSpace(dto.Category) ? "General" : dto.Category.Trim();
        item.SerialNumber = dto.SerialNumber?.Trim();
        item.ModelNumber = dto.ModelNumber?.Trim();
        item.Manufacturer = dto.Manufacturer?.Trim();
        item.RoomOrChair = dto.RoomOrChair?.Trim();
        item.Status = string.IsNullOrWhiteSpace(dto.Status) ? "Operational" : dto.Status.Trim();
        item.PurchaseCost = dto.PurchaseCost;
        item.PurchaseDate = dto.PurchaseDate;
        item.WarrantyExpiryDate = dto.WarrantyExpiryDate;
        item.LastMaintenanceDate = dto.LastMaintenanceDate;
        item.NextMaintenanceDate = dto.NextMaintenanceDate;
        item.MaintenanceNotes = dto.MaintenanceNotes;
        item.ServiceProvider = dto.ServiceProvider;
        item.ServiceContactPhone = dto.ServiceContactPhone;
        item.ImageUrl = dto.ImageUrl?.Trim();

        await _repo.UpdateAsync(item);
        return Ok(new { message = "Equipment updated successfully.", data = MapToDto(item) });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var item = await _repo.GetByIdAsync(id);
        if (item == null) return NotFound(new { message = "Equipment not found" });

        var clinicIdClaim = User.FindFirst("clinicId")?.Value;
        if (!string.IsNullOrEmpty(clinicIdClaim) && item.ClinicId != clinicIdClaim)
        {
            return StatusCode(403, new { message = "You can only delete equipment for your assigned clinic" });
        }

        await _repo.DeleteAsync(id);
        return Ok(new { message = "Equipment deleted successfully." });
    }

    [HttpPost("{id}/maintenance")]
    public async Task<IActionResult> LogMaintenance(string id, [FromBody] EquipmentMaintenanceLogRequest req)
    {
        var item = await _repo.GetByIdAsync(id);
        if (item == null) return NotFound(new { message = "Equipment not found" });

        var clinicIdClaim = User.FindFirst("clinicId")?.Value;
        if (!string.IsNullOrEmpty(clinicIdClaim) && item.ClinicId != clinicIdClaim)
        {
            return StatusCode(403, new { message = "You can only log maintenance for your assigned clinic" });
        }

        item.LastMaintenanceDate = req.ServiceDate;
        if (req.NextDueDate.HasValue) item.NextMaintenanceDate = req.NextDueDate.Value;
        if (!string.IsNullOrWhiteSpace(req.Status)) item.Status = req.Status.Trim();
        if (!string.IsNullOrWhiteSpace(req.Notes)) item.MaintenanceNotes = req.Notes.Trim();
        if (!string.IsNullOrWhiteSpace(req.ServiceProvider)) item.ServiceProvider = req.ServiceProvider.Trim();
        if (!string.IsNullOrWhiteSpace(req.ServiceContactPhone)) item.ServiceContactPhone = req.ServiceContactPhone.Trim();

        await _repo.UpdateAsync(item);
        return Ok(new { message = "Maintenance record saved.", data = MapToDto(item) });
    }

    [HttpPost("seed-defaults")]
    public async Task<IActionResult> SeedDefaults([FromQuery] string clinicId)
    {
        if (string.IsNullOrEmpty(clinicId) || clinicId == "all")
        {
            return BadRequest(new { message = "A specific clinic identifier is required." });
        }

        var existing = await _repo.GetByClinicIdAsync(clinicId);
        if (existing.Any())
        {
            return Ok(new { message = "Clinic already has equipment records.", data = existing.Select(MapToDto) });
        }

        var defaults = new List<Equipment>
        {
            new Equipment
            {
                ClinicId = clinicId,
                Name = "Midmark M11 UltraClave Autoclave",
                Category = "Sterilization",
                Manufacturer = "Midmark",
                ModelNumber = "M11-042",
                SerialNumber = "SN-AUT-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                RoomOrChair = "Sterilization Room",
                Status = "Operational",
                ImageUrl = "/images/equipment/autoclave.jpg",
                NextMaintenanceDate = DateTime.UtcNow.AddMonths(3),
                MaintenanceNotes = "Monthly spore test and door gasket inspection required."
            },
            new Equipment
            {
                ClinicId = clinicId,
                Name = "A-dec 500 Dental Operatory Chair Unit",
                Category = "Operatory",
                Manufacturer = "A-dec",
                ModelNumber = "A-dec 500",
                SerialNumber = "SN-CHR-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                RoomOrChair = "Operatory 1",
                Status = "Operational",
                ImageUrl = "/images/equipment/dental-chair.jpg",
                NextMaintenanceDate = DateTime.UtcNow.AddMonths(6),
                MaintenanceNotes = "Hydraulic pressure and suction canister filter check."
            },
            new Equipment
            {
                ClinicId = clinicId,
                Name = "COXO High-Speed Optic Handpiece",
                Category = "Handpieces",
                Manufacturer = "COXO",
                ModelNumber = "CX207-F",
                SerialNumber = "SN-HP-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                RoomOrChair = "Operatory 1",
                Status = "Operational",
                ImageUrl = "/images/equipment/handpiece.jpg",
                NextMaintenanceDate = DateTime.UtcNow.AddMonths(1),
                MaintenanceNotes = "Turbine lubrication cycle and push-button chuck test."
            },
            new Equipment
            {
                ClinicId = clinicId,
                Name = "Woodpecker LED.B Wireless Curing Light",
                Category = "Curing & Lights",
                Manufacturer = "Woodpecker",
                ModelNumber = "LED.B",
                SerialNumber = "SN-LGT-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                RoomOrChair = "Operatory 1",
                Status = "Operational",
                ImageUrl = "/images/equipment/curing-light.jpg",
                NextMaintenanceDate = DateTime.UtcNow.AddMonths(6),
                MaintenanceNotes = "Radiometer intensity test (minimum 1200 mW/cm²)."
            },
            new Equipment
            {
                ClinicId = clinicId,
                Name = "Dentsply Sirona Cavitron Ultrasonic Scaler",
                Category = "Operatory",
                Manufacturer = "Dentsply Sirona",
                ModelNumber = "Plus-30K",
                SerialNumber = "SN-SCL-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                RoomOrChair = "Operatory 1",
                Status = "Operational",
                ImageUrl = "/images/equipment/ultrasonic-scaler.jpg",
                NextMaintenanceDate = DateTime.UtcNow.AddMonths(4),
                MaintenanceNotes = "Handpiece cable and water line disinfection."
            }
        };

        await _repo.AddRangeAsync(defaults);
        return Ok(new { message = "Default clinic equipment catalog seeded successfully.", data = defaults.Select(MapToDto) });
    }

    private static EquipmentDto MapToDto(Equipment e) => new()
    {
        Id = e.Id,
        ClinicId = e.ClinicId,
        DoctorId = e.DoctorId,
        Name = e.Name,
        Category = e.Category,
        SerialNumber = e.SerialNumber,
        ModelNumber = e.ModelNumber,
        Manufacturer = e.Manufacturer,
        RoomOrChair = e.RoomOrChair,
        Status = e.Status,
        PurchaseCost = e.PurchaseCost,
        PurchaseDate = e.PurchaseDate,
        WarrantyExpiryDate = e.WarrantyExpiryDate,
        LastMaintenanceDate = e.LastMaintenanceDate,
        NextMaintenanceDate = e.NextMaintenanceDate,
        MaintenanceNotes = e.MaintenanceNotes,
        ServiceProvider = e.ServiceProvider,
        ServiceContactPhone = e.ServiceContactPhone,
        ImageUrl = e.ImageUrl,
        CreatedAt = e.CreatedAt,
        IsMaintenanceDue = e.IsMaintenanceDue,
        IsWarrantyExpired = e.IsWarrantyExpired
    };
}
