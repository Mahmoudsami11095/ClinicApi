using System;
using System.Linq;
using System.Threading.Tasks;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/ai-no-show")]
[Authorize(Roles = "admin,doctor,assistant")]
public class AiNoShowController : ControllerBase
{
    private readonly ClinicDbContext _context;
    private readonly IWhatsAppNotificationService _whatsAppService;

    public AiNoShowController(ClinicDbContext context, IWhatsAppNotificationService whatsAppService)
    {
        _context = context;
        _whatsAppService = whatsAppService;
    }

    [HttpPost("predict/{appointmentId}")]
    public async Task<IActionResult> PredictNoShow(string appointmentId)
    {
        var appt = await _context.Appointments
            .Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);
            
        if (appt == null) return NotFound();

        // Simulate AI heuristic calculation (BR-AI-NOSHOW-01)
        var score = 30.0;
        var leadTimeDays = (DateTime.Parse(appt.Date) - DateTime.UtcNow).TotalDays;
        
        if (leadTimeDays > 14) score += 20; // Long lead time = higher risk
        if (appt.Patient != null)
        {
            var totalAppts = await _context.Appointments.CountAsync(a => a.PatientId == appt.PatientId);
            var cancelledAppts = await _context.Appointments.CountAsync(a => a.PatientId == appt.PatientId && a.Status == "cancelled");
            if (totalAppts > 0)
            {
                var cancelRate = (double)cancelledAppts / totalAppts;
                score += (cancelRate * 50); // Historical cancellation rate weight
            }
        }

        appt.NoShowPredictionScore = Math.Min(100, Math.Max(0, score));
        appt.IsHighRiskNoShow = appt.NoShowPredictionScore > 70.0;

        await _context.SaveChangesAsync();
        return Ok(new { predictionScore = appt.NoShowPredictionScore, isHighRisk = appt.IsHighRiskNoShow });
    }

    [HttpPost("standby/{clinicId}")]
    public async Task<IActionResult> JoinStandbyQueue(string clinicId, [FromBody] JoinStandbyRequest request)
    {
        var entry = new StandbyQueueEntry
        {
            ClinicId = clinicId,
            DoctorId = request.DoctorId,
            PatientId = request.PatientId,
            RequestedDate = request.RequestedDate,
            Status = "Waiting"
        };

        _context.StandbyQueue.Add(entry);
        await _context.SaveChangesAsync();

        return Ok(entry);
    }

    [HttpPost("trigger-standby/{appointmentId}")]
    public async Task<IActionResult> TriggerStandby(string appointmentId)
    {
        var appt = await _context.Appointments
            .Include(a => a.Clinic)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);
            
        if (appt == null) return NotFound();

        // Check if there are waitlisted patients (BR-AI-NOSHOW-02)
        var waitlisted = await _context.StandbyQueue
            .Include(q => q.Patient)
            .Where(q => q.ClinicId == appt.ClinicId && 
                        q.DoctorId == appt.DoctorId && 
                        q.RequestedDate == appt.Date.Substring(0, 10) && 
                        q.Status == "Waiting")
            .OrderBy(q => q.CreatedAt)
            .ToListAsync();

        int notified = 0;
        foreach (var entry in waitlisted)
        {
            try
            {
                var msg = $"A slot just opened up with Dr. {appt.Doctor.LastName} on {entry.RequestedDate}. Reply 'Confirm' to grab it.";
                await _whatsAppService.SendNotificationAsync(entry.Patient.PhoneNumber, msg);
                entry.Status = "Notified";
                entry.NotifiedAt = DateTime.UtcNow;
                notified++;
            }
            catch { }
        }

        await _context.SaveChangesAsync();
        return Ok(new { notifiedCount = notified });
    }
}

public class JoinStandbyRequest
{
    public string DoctorId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string RequestedDate { get; set; } = string.Empty;
}
