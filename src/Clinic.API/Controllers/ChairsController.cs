using Clinic.API.Hubs;
using Clinic.Application.DTOs;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin,doctor,assistant,receptionist")]
public class ChairsController : ControllerBase
{
    private readonly ClinicDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;

    public ChairsController(ClinicDbContext context, IHubContext<NotificationHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetChairs([FromQuery] string? clinicId)
    {
        var targetClinicId = clinicId;
        if (string.IsNullOrEmpty(targetClinicId) || targetClinicId == "all")
        {
            var firstClinic = await _context.Clinics.FirstOrDefaultAsync();
            targetClinicId = firstClinic?.Id ?? "default-clinic";
        }

        var chairs = await _context.ClinicChairs
            .Where(c => c.ClinicId == targetClinicId)
            .OrderBy(c => c.RoomNumber)
            .ThenBy(c => c.ChairName)
            .ToListAsync();

        // Seed 4 standard operatories if none exist for this clinic
        if (!chairs.Any())
        {
            var seedChairs = new List<ClinicChair>
            {
                new() { ClinicId = targetClinicId, RoomNumber = "101", ChairName = "Operatory 1 - Primary Care", Status = "available" },
                new() { ClinicId = targetClinicId, RoomNumber = "102", ChairName = "Operatory 2 - Restorative Suite", Status = "available" },
                new() { ClinicId = targetClinicId, RoomNumber = "103", ChairName = "Operatory 3 - Endodontic & Surgical", Status = "available" },
                new() { ClinicId = targetClinicId, RoomNumber = "104", ChairName = "Hygiene Suite - Prophylaxis", Status = "available" }
            };

            await _context.ClinicChairs.AddRangeAsync(seedChairs);
            await _context.SaveChangesAsync();
            chairs = seedChairs;
        }

        return Ok(chairs);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateChairStatusDto dto)
    {
        var chair = await _context.ClinicChairs.FindAsync(id);
        if (chair == null)
        {
            return NotFound(new { message = "Chair not found" });
        }

        var validStatuses = new[] { "available", "occupied", "cleaning", "maintenance" };
        if (!validStatuses.Contains(dto.Status.ToLower()))
        {
            return BadRequest(new { message = "Invalid status. Must be available, occupied, cleaning, or maintenance." });
        }

        chair.Status = dto.Status.ToLower();
        if (dto.Notes != null)
        {
            chair.Notes = dto.Notes;
        }
        chair.UpdatedAt = DateTime.UtcNow;

        if (chair.Status == "cleaning")
        {
            chair.CleaningStartedAt = DateTime.UtcNow;
            chair.OccupancyStartedAt = null;
        }
        else if (chair.Status == "available")
        {
            chair.CleaningStartedAt = null;
            chair.OccupancyStartedAt = null;
            chair.CurrentPatientId = null;
            chair.CurrentPatientName = null;
            chair.ProcedureName = null;
        }

        await _context.SaveChangesAsync();
        await BroadcastChairUpdate(chair);

        return Ok(chair);
    }

    [HttpPost("{id}/assign")]
    public async Task<IActionResult> AssignPatient(string id, [FromBody] AssignChairDto dto)
    {
        var chair = await _context.ClinicChairs.FindAsync(id);
        if (chair == null)
        {
            return NotFound(new { message = "Chair not found" });
        }

        chair.Status = "occupied";
        chair.CurrentPatientId = dto.PatientId;
        chair.CurrentPatientName = dto.PatientName;
        chair.CurrentDoctorId = dto.DoctorId;
        chair.CurrentDoctorName = dto.DoctorName;
        chair.ProcedureName = dto.ProcedureName;
        chair.OccupancyStartedAt = DateTime.UtcNow;
        chair.CleaningStartedAt = null;
        if (!string.IsNullOrEmpty(dto.Notes))
        {
            chair.Notes = dto.Notes;
        }
        chair.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await BroadcastChairUpdate(chair);

        return Ok(chair);
    }

    [HttpPost("{id}/release")]
    public async Task<IActionResult> ReleaseChair(string id)
    {
        var chair = await _context.ClinicChairs.FindAsync(id);
        if (chair == null)
        {
            return NotFound(new { message = "Chair not found" });
        }

        chair.Status = "cleaning";
        chair.CleaningStartedAt = DateTime.UtcNow;
        chair.OccupancyStartedAt = null;
        chair.CurrentPatientId = null;
        chair.CurrentPatientName = null;
        chair.ProcedureName = null;
        chair.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await BroadcastChairUpdate(chair);

        return Ok(chair);
    }

    [HttpPost("{id}/complete-cleaning")]
    public async Task<IActionResult> CompleteCleaning(string id)
    {
        var chair = await _context.ClinicChairs.FindAsync(id);
        if (chair == null)
        {
            return NotFound(new { message = "Chair not found" });
        }

        chair.Status = "available";
        chair.CleaningStartedAt = null;
        chair.OccupancyStartedAt = null;
        chair.CurrentPatientId = null;
        chair.CurrentPatientName = null;
        chair.ProcedureName = null;
        chair.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await BroadcastChairUpdate(chair);

        return Ok(chair);
    }

    private async Task BroadcastChairUpdate(ClinicChair chair)
    {
        try
        {
            await _hubContext.Clients.Group($"clinic_{chair.ClinicId}").SendAsync("ReceiveChairStatusUpdate", chair);
            await _hubContext.Clients.All.SendAsync("ReceiveChairStatusUpdate", chair);
        }
        catch
        {
            // Silently swallow SignalR broadcast errors during unit tests or when client disconnected
        }
    }
}
