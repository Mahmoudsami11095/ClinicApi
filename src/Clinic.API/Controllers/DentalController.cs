using Clinic.Application.Common;
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
    private static readonly Dictionary<string, string> AllowedTransitions = new(StringComparer.OrdinalIgnoreCase)
    {
        { "proposed", "accepted" },
        { "accepted", "in_progress" },
        { "in_progress", "completed" }
    };

    private static readonly HashSet<string> ValidStages = new(StringComparer.OrdinalIgnoreCase)
    {
        "proposed", "accepted", "in_progress", "completed", "invoiced"
    };

    private readonly IDentalLogRepository _repo;
    private readonly IMaterialRepository _materialRepo;
    private readonly IClinicRepository _clinicRepo;
    private readonly IPatientRepository _patientRepo;
    private readonly IBillingRepository _billingRepo;
    private readonly IMaterialAlertService? _alertService;

    public DentalController(
        IDentalLogRepository repo,
        IMaterialRepository materialRepo,
        IClinicRepository clinicRepo,
        IPatientRepository patientRepo,
        IBillingRepository billingRepo,
        IMaterialAlertService? alertService = null)
    {
        _repo = repo;
        _materialRepo = materialRepo;
        _clinicRepo = clinicRepo;
        _patientRepo = patientRepo;
        _billingRepo = billingRepo;
        _alertService = alertService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var logs = await _repo.GetAllAsync();
        var doctorIdClaim = User.GetDoctorId();
        var clinicIdClaim = User.GetClinicId();

        if (!string.IsNullOrEmpty(doctorIdClaim) || !string.IsNullOrEmpty(clinicIdClaim))
        {
            if (!string.IsNullOrEmpty(doctorIdClaim))
            {
                var allowedClinicIds = await _clinicRepo.GetAllowedClinicIdsForDoctorAsync(doctorIdClaim);
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

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound(new { message = "Dental record not found" });

        var doctorIdClaim = User.GetDoctorId();
        var clinicIdClaim = User.GetClinicId();
        if (!string.IsNullOrEmpty(doctorIdClaim))
        {
            var isAllowed = !string.IsNullOrEmpty(entity.ClinicId) &&
                            await _clinicRepo.IsDoctorAuthorizedForClinicAsync(doctorIdClaim, entity.ClinicId);
            if (!isAllowed) return StatusCode(403, new { message = "Access denied" });
        }
        else if (!string.IsNullOrEmpty(clinicIdClaim))
        {
            if (entity.ClinicId != clinicIdClaim) return StatusCode(403, new { message = "Access denied" });
        }

        return Ok(new { data = MapToDto(entity) });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DentalLogDto dto)
    {
        var doctorIdClaim = User.GetDoctorId();
        var clinicIdClaim = User.GetClinicId();

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
                var isAllowed = !string.IsNullOrEmpty(dto.ClinicId) && 
                                await _clinicRepo.IsDoctorAuthorizedForClinicAsync(doctorIdClaim, dto.ClinicId);
                if (!isAllowed) return StatusCode(403, new { message = "You can only manage dental logs for your clinics" });
            }
            else if (!string.IsNullOrEmpty(clinicIdClaim))
            {
                if (dto.ClinicId != clinicIdClaim)
                    return StatusCode(403, new { message = "You can only manage dental logs for your assigned clinic" });
            }
        }

        // Guardrail BR-INV-01: Expiration Date Quarantine
        if (dto.ConsumedMaterials != null && dto.ConsumedMaterials.Any())
        {
            foreach (var cm in dto.ConsumedMaterials)
            {
                var material = await _materialRepo.GetByIdAsync(cm.MaterialId);
                if (material != null && material.IsExpired)
                {
                    return BadRequest(new
                    {
                        message = $"Material '{material.Name}' (Batch: {material.BatchNumber ?? "N/A"}) expired on {material.ExpirationDate!.Value:yyyy-MM-dd} and is quarantined from clinical procedures."
                    });
                }
            }
        }

        // BR-DEN-02: Stage initialization and validation
        var stage = string.IsNullOrWhiteSpace(dto.Stage) ? "proposed" : dto.Stage.Trim().ToLowerInvariant();
        if (!ValidStages.Contains(stage))
        {
            return BadRequest(new { message = $"Invalid stage '{dto.Stage}'. Valid stages: proposed, accepted, in_progress, completed, invoiced." });
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
            IsPlanned = stage == "completed" || stage == "invoiced" ? false : dto.IsPlanned,
            Stage = stage,
            Cost = dto.Cost >= 0 ? dto.Cost : 0m,
            InvoiceId = dto.InvoiceId,
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

                    if (_alertService != null)
                    {
                        await _alertService.CheckAndTriggerLowStockAlertAsync(material);
                    }
                }
            }
        }

        return Ok(new { message = "Success", data = MapToDto(entity) });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] DentalLogDto dto)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound(new { message = "Dental record not found" });

        var doctorIdClaim = User.GetDoctorId();
        var clinicIdClaim = User.GetClinicId();
        if (!string.IsNullOrEmpty(doctorIdClaim))
        {
            var isAllowed = !string.IsNullOrEmpty(entity.ClinicId) && 
                            await _clinicRepo.IsDoctorAuthorizedForClinicAsync(doctorIdClaim, entity.ClinicId);
            if (!isAllowed) return StatusCode(403, new { message = "Access denied" });
        }
        else if (!string.IsNullOrEmpty(clinicIdClaim))
        {
            if (entity.ClinicId != clinicIdClaim) return StatusCode(403, new { message = "Access denied" });
        }

        // BR-DEN-02: Invoiced procedures cannot be modified
        if (entity.Stage == "invoiced")
        {
            return BadRequest(new { message = "BR-DEN-02: Invoiced procedures are locked and cannot be edited." });
        }

        // If stage is changing, validate strict sequential progression
        var currentStage = (entity.Stage ?? "proposed").ToLowerInvariant();
        var targetStage = string.IsNullOrWhiteSpace(dto.Stage) ? currentStage : dto.Stage.Trim().ToLowerInvariant();

        if (currentStage != targetStage)
        {
            if (targetStage == "invoiced")
            {
                return BadRequest(new { message = "BR-DEN-02: Procedures cannot be directly set to 'invoiced'. Use POST /api/dental/{id}/push-to-billing to generate an invoice and transition to invoiced." });
            }

            if (!AllowedTransitions.TryGetValue(currentStage, out var nextAllowed) ||
                !string.Equals(targetStage, nextAllowed, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = $"BR-DEN-02: Invalid procedure stage transition from '{currentStage}' to '{targetStage}'. Procedure lifecycle must strictly follow: proposed -> accepted -> in_progress -> completed -> invoiced." });
            }

            entity.Stage = nextAllowed;
            if (nextAllowed == "completed")
            {
                entity.IsPlanned = false;

                // REQ-INV-03: Auto-Deduct Recipe Consumables on Procedure Completion
                if (!string.IsNullOrEmpty(entity.ConsumedMaterials) && entity.ConsumedMaterials != "[]")
                {
                    try
                    {
                        var consumed = JsonSerializer.Deserialize<List<ConsumedMaterialDto>>(entity.ConsumedMaterials);
                        if (consumed != null)
                        {
                            foreach (var cm in consumed)
                            {
                                var material = await _materialRepo.GetByIdAsync(cm.MaterialId);
                                if (material != null)
                                {
                                    material.Quantity -= cm.Quantity;
                                    if (material.Quantity < 0) material.Quantity = 0;
                                    await _materialRepo.UpdateAsync(material);

                                    if (_alertService != null)
                                    {
                                        await _alertService.CheckAndTriggerLowStockAlertAsync(material);
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
        }

        entity.ToothNumber = dto.ToothNumber;
        entity.DoctorId = dto.DoctorId;
        entity.DoctorName = dto.DoctorName;
        entity.Date = dto.Date;
        entity.Status = JsonSerializer.Serialize(dto.Status);
        entity.PainLevel = dto.PainLevel;
        entity.PainDetails = dto.PainDetails;
        entity.Treatment = dto.Treatment;
        entity.Medication = dto.Medication;
        entity.Cost = dto.Cost >= 0 ? dto.Cost : entity.Cost;
        if (entity.Stage != "completed") entity.IsPlanned = dto.IsPlanned;

        await _repo.UpdateAsync(entity);
        return Ok(new { message = "Success", data = MapToDto(entity) });
    }

    // BR-DEN-02: Strict sequential state progression endpoint
    [HttpPut("{id}/stage")]
    public async Task<IActionResult> UpdateStage(string id, [FromBody] UpdateDentalStageDto dto)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound(new { message = "Dental record not found" });

        var doctorIdClaim = User.GetDoctorId();
        var clinicIdClaim = User.GetClinicId();
        if (!string.IsNullOrEmpty(doctorIdClaim))
        {
            var isAllowed = !string.IsNullOrEmpty(entity.ClinicId) && 
                            await _clinicRepo.IsDoctorAuthorizedForClinicAsync(doctorIdClaim, entity.ClinicId);
            if (!isAllowed) return StatusCode(403, new { message = "Access denied" });
        }
        else if (!string.IsNullOrEmpty(clinicIdClaim))
        {
            if (entity.ClinicId != clinicIdClaim) return StatusCode(403, new { message = "Access denied" });
        }

        var currentStage = (entity.Stage ?? "proposed").ToLowerInvariant();
        var targetStage = (dto.Stage ?? "").Trim().ToLowerInvariant();

        if (string.Equals(currentStage, targetStage, StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new { message = "Stage unchanged.", data = MapToDto(entity) });
        }

        if (targetStage == "invoiced")
        {
            return BadRequest(new { message = "BR-DEN-02: Procedures cannot be directly set to 'invoiced'. Use POST /api/dental/{id}/push-to-billing to generate an invoice and transition to invoiced." });
        }

        if (currentStage == "completed")
        {
            return BadRequest(new { message = "BR-DEN-02: Completed procedures can only transition to 'invoiced' via POST /api/dental/{id}/push-to-billing." });
        }

        if (currentStage == "invoiced")
        {
            return BadRequest(new { message = "BR-DEN-02: Invoiced procedures are finalized and cannot transition to any other stage." });
        }

        if (!AllowedTransitions.TryGetValue(currentStage, out var nextAllowed) ||
            !string.Equals(targetStage, nextAllowed, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = $"BR-DEN-02: Invalid procedure stage transition from '{currentStage}' to '{targetStage}'. Procedure lifecycle must strictly follow: proposed -> accepted -> in_progress -> completed -> invoiced." });
        }

        entity.Stage = nextAllowed;
        if (nextAllowed == "completed")
        {
            entity.IsPlanned = false;

            // REQ-INV-03: Auto-Deduct Recipe Consumables on Procedure Completion
            if (!string.IsNullOrEmpty(entity.ConsumedMaterials) && entity.ConsumedMaterials != "[]")
            {
                try
                {
                    var consumed = JsonSerializer.Deserialize<List<ConsumedMaterialDto>>(entity.ConsumedMaterials);
                    if (consumed != null)
                    {
                        foreach (var cm in consumed)
                        {
                            var material = await _materialRepo.GetByIdAsync(cm.MaterialId);
                            if (material != null)
                            {
                                material.Quantity -= cm.Quantity;
                                if (material.Quantity < 0) material.Quantity = 0;
                                await _materialRepo.UpdateAsync(material);

                                if (_alertService != null)
                                {
                                    await _alertService.CheckAndTriggerLowStockAlertAsync(material);
                                }
                            }
                        }
                    }
                }
                catch { }
            }
        }

        await _repo.UpdateAsync(entity);
        return Ok(new { message = $"Procedure advanced to '{nextAllowed}'.", data = MapToDto(entity) });
    }

    // BR-DEN-02: Invoicing Guardrail — only "completed" procedures can be pushed to billing
    [HttpPost("{id}/push-to-billing")]
    public async Task<IActionResult> PushToBilling(string id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound(new { message = "Dental record not found" });

        var doctorIdClaim = User.GetDoctorId();
        var clinicIdClaim = User.GetClinicId();
        if (!string.IsNullOrEmpty(doctorIdClaim))
        {
            var isAllowed = !string.IsNullOrEmpty(entity.ClinicId) && 
                            await _clinicRepo.IsDoctorAuthorizedForClinicAsync(doctorIdClaim, entity.ClinicId);
            if (!isAllowed) return StatusCode(403, new { message = "Access denied" });
        }
        else if (!string.IsNullOrEmpty(clinicIdClaim))
        {
            if (entity.ClinicId != clinicIdClaim) return StatusCode(403, new { message = "Access denied" });
        }

        var currentStage = (entity.Stage ?? "proposed").ToLowerInvariant();

        if (currentStage == "invoiced" || !string.IsNullOrEmpty(entity.InvoiceId))
        {
            return BadRequest(new { message = "BR-DEN-02: This dental procedure has already been invoiced." });
        }

        // Guardrail: Only procedures in the Completed status can be pushed into the billing module
        if (currentStage != "completed")
        {
            return BadRequest(new
            {
                message = $"BR-DEN-02: Only procedures in the 'completed' status can be pushed into the billing module for cashier settlement. Current status is '{currentStage}'."
            });
        }

        // Generate sequential gapless invoice number per clinic (BR-FIN-03)
        var invoiceNumber = await _billingRepo.GetNextInvoiceNumberAsync(entity.ClinicId);
        var cost = entity.Cost > 0 ? entity.Cost : 0m;

        var invoice = new BillingRecord
        {
            Id = Guid.NewGuid().ToString(),
            PatientId = entity.PatientId,
            InvoiceNumber = invoiceNumber,
            Subtotal = cost,
            DiscountPercentage = 0,
            DiscountAmount = 0,
            Amount = cost,
            PaidAmount = 0,
            Status = "pending",
            DateIssued = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Description = $"Tooth #{entity.ToothNumber} - {(string.IsNullOrWhiteSpace(entity.Treatment) ? "Dental Procedure" : entity.Treatment)}",
            ClinicId = entity.ClinicId
        };

        await _billingRepo.AddAsync(invoice);

        // Update dental log state to invoiced and link invoice
        entity.InvoiceId = invoice.Id;
        entity.Stage = "invoiced";
        await _repo.UpdateAsync(entity);

        return Ok(new
        {
            message = "Procedure successfully pushed to billing.",
            data = MapToDto(entity),
            invoice = new
            {
                id = invoice.Id,
                invoiceNumber = invoice.InvoiceNumber,
                amount = invoice.Amount,
                status = invoice.Status,
                dateIssued = invoice.DateIssued,
                description = invoice.Description,
                patientId = invoice.PatientId,
                clinicId = invoice.ClinicId
            }
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound(new { message = "Dental record not found" });

        if (entity.Stage == "invoiced")
        {
            return BadRequest(new { message = "BR-DEN-02: Invoiced procedures cannot be deleted." });
        }

        await _repo.DeleteAsync(id);
        return Ok(new { message = "Success" });
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
            Id = d.Id,
            PatientId = d.PatientId,
            ToothNumber = d.ToothNumber,
            DoctorId = d.DoctorId,
            DoctorName = d.DoctorName,
            Date = d.Date,
            Status = statusList,
            PainLevel = d.PainLevel,
            PainDetails = d.PainDetails,
            Treatment = d.Treatment,
            Medication = d.Medication,
            IsPlanned = d.IsPlanned,
            Stage = string.IsNullOrWhiteSpace(d.Stage) ? "proposed" : d.Stage,
            Cost = d.Cost,
            InvoiceId = d.InvoiceId,
            ConsumedMaterials = materialsList,
            ClinicId = d.ClinicId
        };
    }
}
