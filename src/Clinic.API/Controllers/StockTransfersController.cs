using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/inventory/transfers")]
[Authorize(Roles = "admin,doctor,assistant")]
public class StockTransfersController : ControllerBase
{
    private readonly ClinicDbContext _context;
    private readonly IStockTransferRequisitionRepository _transferRepo;
    private readonly INotificationService _notificationService;

    public StockTransfersController(
        ClinicDbContext context,
        IStockTransferRequisitionRepository transferRepo,
        INotificationService notificationService)
    {
        _context = context;
        _transferRepo = transferRepo;
        _notificationService = notificationService;
    }

    [HttpPost("request")]
    public async Task<IActionResult> RequestTransfer([FromBody] CreateStockTransferRequestDto dto)
    {
        if (dto.SourceClinicId == dto.DestinationClinicId)
            return BadRequest(new { message = "Source and destination clinics must be different facilities." });

        var sourceClinic = await _context.Clinics.FindAsync(dto.SourceClinicId);
        if (sourceClinic == null)
            return NotFound(new { message = "Source clinic not found." });

        var destClinic = await _context.Clinics.FindAsync(dto.DestinationClinicId);
        if (destClinic == null)
            return NotFound(new { message = "Destination clinic not found." });

        var material = await _context.Materials.FindAsync(dto.MaterialId);
        if (material == null)
            return NotFound(new { message = "Requested material not found." });

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system-user";
        var requisitionNumber = await _transferRepo.GetNextRequisitionNumberAsync();

        var requisition = new StockTransferRequisition
        {
            Id = Guid.NewGuid().ToString(),
            RequisitionNumber = requisitionNumber,
            SourceClinicId = dto.SourceClinicId,
            DestinationClinicId = dto.DestinationClinicId,
            MaterialId = dto.MaterialId,
            QuantityRequested = dto.QuantityRequested,
            Priority = string.IsNullOrWhiteSpace(dto.Priority) ? "Normal" : dto.Priority,
            Status = "Requested",
            RequestedByUserId = currentUserId,
            RequestedAt = DateTime.UtcNow,
            Notes = dto.Notes
        };

        await _transferRepo.AddAsync(requisition);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = requisition.Id }, MapToDto(requisition, sourceClinic.Name, destClinic.Name, material.Name));
    }

    [HttpGet]
    public async Task<IActionResult> GetTransfers(
        [FromQuery] string? clinicId,
        [FromQuery] string? status,
        [FromQuery] string? direction = "all")
    {
        if (string.IsNullOrEmpty(clinicId) || clinicId == "all")
        {
            var allQuery = _context.StockTransferRequisitions
                .Include(t => t.SourceClinic)
                .Include(t => t.DestinationClinic)
                .Include(t => t.Material)
                .Where(t => !t.IsDeleted);

            if (!string.IsNullOrEmpty(status) && status != "all")
                allQuery = allQuery.Where(t => t.Status == status);

            var allList = await allQuery.OrderByDescending(t => t.RequestedAt).AsNoTracking().ToListAsync();
            return Ok(new { data = allList.Select(t => MapToDto(t, t.SourceClinic?.Name, t.DestinationClinic?.Name, t.Material?.Name)) });
        }

        var list = await _transferRepo.GetByClinicAsync(clinicId, status, direction);
        return Ok(new { data = list.Select(t => MapToDto(t, t.SourceClinic?.Name, t.DestinationClinic?.Name, t.Material?.Name)) });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var item = await _transferRepo.GetByIdAsync(id);
        if (item == null)
            return NotFound(new { message = "Stock transfer requisition not found." });

        return Ok(new { data = MapToDto(item, item.SourceClinic?.Name, item.DestinationClinic?.Name, item.Material?.Name) });
    }

    [HttpPut("{id}/approve")]
    public async Task<IActionResult> ApproveTransfer(string id, [FromBody] ApproveStockTransferDto dto)
    {
        var requisition = await _transferRepo.GetByIdAsync(id);
        if (requisition == null)
            return NotFound(new { message = "Transfer requisition not found." });

        if (requisition.Status != "Requested")
            return BadRequest(new { message = $"Cannot approve requisition in status '{requisition.Status}'." });

        // BR-LOG-03: FEFO Shelf-Life Guardrail (< 30 days)
        if (dto.ExpiryDate.HasValue && dto.ExpiryDate.Value.Date < DateTime.UtcNow.AddDays(30).Date)
        {
            if (string.IsNullOrWhiteSpace(dto.Notes) || !dto.Notes.Contains("OVERRIDE", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "BR-LOG-03: Consumables with less than 30 days remaining shelf life cannot be dispatched without clinical supervisor override." });
            }
        }

        requisition.Status = "Approved";
        if (!string.IsNullOrEmpty(dto.BatchNumber)) requisition.BatchNumber = dto.BatchNumber;
        if (dto.ExpiryDate.HasValue) requisition.ExpiryDate = dto.ExpiryDate.Value;
        if (!string.IsNullOrEmpty(dto.Notes)) requisition.Notes = dto.Notes;

        await _transferRepo.UpdateAsync(requisition);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Transfer requisition approved.", data = MapToDto(requisition, requisition.SourceClinic?.Name, requisition.DestinationClinic?.Name, requisition.Material?.Name) });
    }

    [HttpPut("{id}/dispatch")]
    public async Task<IActionResult> DispatchTransfer(string id, [FromBody] DispatchStockTransferDto dto)
    {
        var requisition = await _transferRepo.GetByIdAsync(id);
        if (requisition == null)
            return NotFound(new { message = "Transfer requisition not found." });

        if (requisition.Status != "Approved" && requisition.Status != "Requested")
            return BadRequest(new { message = $"Cannot dispatch requisition in status '{requisition.Status}'." });

        var sourceMaterial = await _context.Materials.FindAsync(requisition.MaterialId);
        if (sourceMaterial == null)
            return NotFound(new { message = "Source material record not found." });

        if (sourceMaterial.Quantity < dto.QuantityDispatched)
            return BadRequest(new { message = $"Insufficient source stock. Available: {sourceMaterial.Quantity}, Requested for dispatch: {dto.QuantityDispatched}." });

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system-user";

        // BR-LOG-01: Deduct from source facility strictly upon dispatch
        sourceMaterial.Quantity -= dto.QuantityDispatched;

        requisition.Status = "InTransit";
        requisition.QuantityDispatched = dto.QuantityDispatched;
        requisition.DispatchedByUserId = currentUserId;
        requisition.DispatchedAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(dto.BatchNumber)) requisition.BatchNumber = dto.BatchNumber;
        if (dto.ExpiryDate.HasValue) requisition.ExpiryDate = dto.ExpiryDate.Value;
        if (!string.IsNullOrEmpty(dto.Notes)) requisition.Notes = dto.Notes;

        await _transferRepo.UpdateAsync(requisition);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Transfer requisition dispatched. Goods are in transit.", data = MapToDto(requisition, requisition.SourceClinic?.Name, requisition.DestinationClinic?.Name, requisition.Material?.Name) });
    }

    [HttpPut("{id}/receive")]
    public async Task<IActionResult> ReceiveTransfer(string id, [FromBody] ReceiveStockTransferDto dto)
    {
        var requisition = await _transferRepo.GetByIdAsync(id);
        if (requisition == null)
            return NotFound(new { message = "Transfer requisition not found." });

        if (requisition.Status != "InTransit")
            return BadRequest(new { message = $"Cannot receive transfer in status '{requisition.Status}'." });

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system-user";

        // BR-LOG-02: Credit destination inventory only upon physical inspection check
        var effectiveUsableQuantity = Math.Max(0, dto.QuantityReceived - dto.QuantityDamaged);

        // Find or create material in destination clinic
        var sourceMaterial = await _context.Materials.FindAsync(requisition.MaterialId);
        var destMaterial = await _context.Materials.FirstOrDefaultAsync(m =>
            m.ClinicId == requisition.DestinationClinicId &&
            m.Name == (sourceMaterial != null ? sourceMaterial.Name : requisition.Material.Name));

        if (destMaterial != null)
        {
            destMaterial.Quantity += effectiveUsableQuantity;
            if (requisition.ExpiryDate.HasValue) destMaterial.ExpirationDate = requisition.ExpiryDate.Value;
            if (!string.IsNullOrEmpty(requisition.BatchNumber)) destMaterial.BatchNumber = requisition.BatchNumber;
        }
        else if (sourceMaterial != null)
        {
            var newMaterial = new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = requisition.DestinationClinicId,
                DoctorId = sourceMaterial.DoctorId,
                Name = sourceMaterial.Name,
                Category = sourceMaterial.Category,
                Unit = sourceMaterial.Unit,
                Quantity = effectiveUsableQuantity,
                MinStockAlert = sourceMaterial.MinStockAlert,
                ExpirationDate = requisition.ExpiryDate ?? sourceMaterial.ExpirationDate,
                BatchNumber = requisition.BatchNumber ?? sourceMaterial.BatchNumber
            };
            await _context.Materials.AddAsync(newMaterial);
        }

        requisition.Status = "Received";
        requisition.QuantityReceived = dto.QuantityReceived;
        requisition.QuantityDamaged = dto.QuantityDamaged;
        requisition.DamageReason = dto.DamageReason;
        requisition.ReceivedByUserId = currentUserId;
        requisition.ReceivedAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(dto.Notes)) requisition.Notes = dto.Notes;

        await _transferRepo.UpdateAsync(requisition);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Shipment received and credited to destination clinic inventory.", data = MapToDto(requisition, requisition.SourceClinic?.Name, requisition.DestinationClinic?.Name, requisition.Material?.Name) });
    }

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelTransfer(string id)
    {
        var requisition = await _transferRepo.GetByIdAsync(id);
        if (requisition == null)
            return NotFound(new { message = "Transfer requisition not found." });

        if (requisition.Status == "InTransit" || requisition.Status == "Received")
            return BadRequest(new { message = $"Cannot cancel transfer in status '{requisition.Status}'." });

        requisition.Status = "Cancelled";
        await _transferRepo.UpdateAsync(requisition);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Transfer requisition cancelled.", data = MapToDto(requisition, requisition.SourceClinic?.Name, requisition.DestinationClinic?.Name, requisition.Material?.Name) });
    }

    private static StockTransferRequisitionResponseDto MapToDto(
        StockTransferRequisition t,
        string? sourceClinicName,
        string? destClinicName,
        string? materialName)
    {
        return new StockTransferRequisitionResponseDto
        {
            Id = t.Id,
            RequisitionNumber = t.RequisitionNumber,
            SourceClinicId = t.SourceClinicId,
            SourceClinicName = sourceClinicName ?? t.SourceClinicId,
            DestinationClinicId = t.DestinationClinicId,
            DestinationClinicName = destClinicName ?? t.DestinationClinicId,
            MaterialId = t.MaterialId,
            MaterialName = materialName ?? t.MaterialId,
            QuantityRequested = t.QuantityRequested,
            QuantityDispatched = t.QuantityDispatched,
            QuantityReceived = t.QuantityReceived,
            QuantityDamaged = t.QuantityDamaged,
            BatchNumber = t.BatchNumber,
            ExpiryDate = t.ExpiryDate,
            Priority = t.Priority,
            Status = t.Status,
            RequestedByUserId = t.RequestedByUserId,
            DispatchedByUserId = t.DispatchedByUserId,
            ReceivedByUserId = t.ReceivedByUserId,
            RequestedAt = t.RequestedAt,
            DispatchedAt = t.DispatchedAt,
            ReceivedAt = t.ReceivedAt,
            Notes = t.Notes,
            DamageReason = t.DamageReason
        };
    }
}
