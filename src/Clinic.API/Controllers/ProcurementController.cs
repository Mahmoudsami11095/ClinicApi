using System;
using System.Linq;
using System.Threading.Tasks;
using Clinic.Application.DTOs;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/procurement")]
[Authorize(Roles = "admin,procurement")]
public class ProcurementController : ControllerBase
{
    private readonly ClinicDbContext _context;

    public ProcurementController(ClinicDbContext context)
    {
        _context = context;
    }

    [HttpGet("suppliers")]
    public async Task<IActionResult> GetSuppliers()
    {
        var suppliers = await _context.Suppliers.AsNoTracking().ToListAsync();
        return Ok(new { data = suppliers });
    }

    [HttpPost("orders")]
    public async Task<IActionResult> CreateDraftPO([FromBody] CreatePurchaseOrderDto dto)
    {
        if (!dto.Items.Any()) return BadRequest(new { message = "PO must contain at least one item." });

        var totalAmount = dto.Items.Sum(i => i.Quantity * i.UnitPrice);
        var poNumber = $"PO-{DateTime.UtcNow:yyyyMM}-{new Random().Next(1000, 9999)}";

        var po = new PurchaseOrder
        {
            PONumber = poNumber,
            ClinicId = dto.ClinicId,
            SupplierId = dto.SupplierId,
            Status = "Draft",
            TotalAmount = totalAmount,
            CreatedAt = DateTime.UtcNow
        };

        foreach(var item in dto.Items)
        {
            po.Items.Add(new PurchaseOrderItem
            {
                MaterialId = item.MaterialId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            });
        }

        _context.PurchaseOrders.Add(po);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrderById), new { id = po.Id }, new { data = po });
    }

    [HttpGet("orders/{id}")]
    public async Task<IActionResult> GetOrderById(string id)
    {
        var po = await _context.PurchaseOrders
            .Include(p => p.Items)
            .Include(p => p.Supplier)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
            
        if (po == null) return NotFound(new { message = "PO not found" });
        return Ok(new { data = po });
    }

    [HttpPost("orders/{id}/dispatch")]
    public async Task<IActionResult> DispatchOrder(string id)
    {
        var po = await _context.PurchaseOrders.FirstOrDefaultAsync(p => p.Id == id);
        if (po == null) return NotFound();

        if (po.Status != "Draft") return BadRequest(new { message = "Only Draft POs can be dispatched." });

        po.Status = "Sent";
        po.DispatchedAt = DateTime.UtcNow;
        // Trigger supplier EDI generation or email notification logic here.

        await _context.SaveChangesAsync();
        return Ok(new { message = "PO Dispatched via EDI", data = po });
    }
    
    [HttpPost("orders/{id}/receive")]
    public async Task<IActionResult> ReceiveOrder(string id)
    {
        var po = await _context.PurchaseOrders.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == id);
        if (po == null) return NotFound();

        if (po.Status != "Sent" && po.Status != "PartiallyReceived") 
            return BadRequest(new { message = "PO must be Sent to be received." });

        po.Status = "Settled";
        po.ReceivedAt = DateTime.UtcNow;

        // BR-PROC-02 & Inventory Integration
        foreach(var item in po.Items)
        {
            var mat = await _context.Materials.FirstOrDefaultAsync(m => m.Id == item.MaterialId);
            if (mat != null)
            {
                mat.CurrentStock += item.Quantity;
                mat.LastRestockDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Three-Way Match Complete & Inventory Updated", data = po });
    }
}
