using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostOpTriageController : ControllerBase
{
    private static readonly ConcurrentDictionary<string, PostOpTriageSession> _sessions = new();
    private readonly IWhatsAppNotificationService _whatsAppService;

    static PostOpTriageController()
    {
        // Seed initial post-op surgical cases for interactive testing & triage
        var session1 = new PostOpTriageSession
        {
            Id = "postop-seed-101",
            ClinicId = "clinic-main",
            PatientId = "pat-101",
            PatientName = "Sarah Jenkins",
            PatientPhone = "+15550192834",
            ProcedureName = "Surgical Extraction of Impacted Wisdom Tooth #38",
            ProcedureDate = DateTime.UtcNow.AddDays(-1).Date,
            IntervalHours = 24,
            TriageStatus = "Escalated",
            ScheduledDispatchUtc = DateTime.UtcNow.AddHours(-6),
            DispatchedAtUtc = DateTime.UtcNow.AddHours(-6),
            RespondedAtUtc = DateTime.UtcNow.AddHours(-5),
            PainScore = 9,
            BleedingSeverity = "Severe",
            SwellingSeverity = "Severe",
            PatientFeedback = "Severe throbbing pain keeping me awake. Gauze pad is soaked every 20 minutes.",
            IsEscalated = true,
            EscalationReason = "Severe pain level reported (9/10); Persistent severe active bleeding; Significant acute facial/intraoral swelling",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
        };

        var session2 = new PostOpTriageSession
        {
            Id = "postop-seed-102",
            ClinicId = "clinic-main",
            PatientId = "pat-102",
            PatientName = "Michael Chang",
            PatientPhone = "+15550293847",
            ProcedureName = "Dental Implant Placement #46 (Titanium Fixture)",
            ProcedureDate = DateTime.UtcNow.AddDays(-1).Date,
            IntervalHours = 24,
            TriageStatus = "Recovering",
            ScheduledDispatchUtc = DateTime.UtcNow.AddHours(-8),
            DispatchedAtUtc = DateTime.UtcNow.AddHours(-8),
            RespondedAtUtc = DateTime.UtcNow.AddHours(-7),
            PainScore = 3,
            BleedingSeverity = "None",
            SwellingSeverity = "Moderate",
            PatientFeedback = "Pain is well managed with prescribed Ibuprofen. Mild cheek swelling but overall comfortable.",
            IsEscalated = false,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
        };

        var session3 = new PostOpTriageSession
        {
            Id = "postop-seed-103",
            ClinicId = "clinic-main",
            PatientId = "pat-103",
            PatientName = "Emily Rodriguez",
            PatientPhone = "+15550384756",
            ProcedureName = "Apicoectomy & Retrograde Filling #21",
            ProcedureDate = DateTime.UtcNow.Date,
            IntervalHours = 48,
            TriageStatus = "Scheduled",
            ScheduledDispatchUtc = DateTime.UtcNow.AddHours(24),
            CreatedAtUtc = DateTime.UtcNow
        };

        _sessions[session1.Id] = session1;
        _sessions[session2.Id] = session2;
        _sessions[session3.Id] = session3;
    }

    public PostOpTriageController(IWhatsAppNotificationService whatsAppService)
    {
        _whatsAppService = whatsAppService;
    }

    public record TriggerTriageRequest(
        string? PatientId,
        string? PatientName,
        string? PatientPhone,
        string? ProcedureName,
        int? IntervalHours,
        bool ImmediateDispatch
    );

    public record SimulateResponseRequest(
        int? PainScore,
        string? BleedingSeverity,
        string? SwellingSeverity,
        string? PatientFeedback
    );

    public record ResolveTriageRequest(
        string? DoctorClinicalAction
    );

    [HttpGet]
    [Authorize]
    public IActionResult GetTriageSessions([FromQuery] bool? escalatedOnly, [FromQuery] string? status)
    {
        var query = _sessions.Values.AsEnumerable();

        if (escalatedOnly == true)
        {
            query = query.Where(s => s.IsEscalated);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(s => string.Equals(s.TriageStatus, status, StringComparison.OrdinalIgnoreCase));
        }

        var results = query.OrderByDescending(s => s.IsEscalated)
                           .ThenByDescending(s => s.ScheduledDispatchUtc)
                           .ToList();

        return Ok(results);
    }

    [HttpGet("{id}")]
    [Authorize]
    public IActionResult GetTriageSessionById(string id)
    {
        if (_sessions.TryGetValue(id, out var session))
        {
            return Ok(session);
        }
        return NotFound(new { message = $"Triage session {id} not found." });
    }

    [HttpPost("trigger")]
    [Authorize]
    public async Task<IActionResult> TriggerTriageSession([FromBody] TriggerTriageRequest request)
    {
        var session = new PostOpTriageSession
        {
            Id = $"postop-{Guid.NewGuid():N}"[..16],
            ClinicId = "clinic-main",
            PatientId = request.PatientId ?? "patient-default",
            PatientName = request.PatientName ?? "Patient",
            PatientPhone = request.PatientPhone ?? "+15550000000",
            ProcedureName = request.ProcedureName ?? "Surgical Dental Procedure",
            IntervalHours = request.IntervalHours ?? 24,
            ProcedureDate = DateTime.UtcNow.Date,
            ScheduledDispatchUtc = request.ImmediateDispatch ? DateTime.UtcNow : DateTime.UtcNow.AddHours(request.IntervalHours ?? 24),
            TriageStatus = request.ImmediateDispatch ? "PendingResponse" : "Scheduled",
            DispatchedAtUtc = request.ImmediateDispatch ? DateTime.UtcNow : null
        };

        _sessions[session.Id] = session;

        if (request.ImmediateDispatch && !string.IsNullOrEmpty(session.PatientPhone))
        {
            var msg = $"Hello {session.PatientName}, this is Smart Clinic checking in on your recovery after your {session.ProcedureName}. Please reply with your current pain level (1-10).";
            await _whatsAppService.SendNotificationAsync(session.PatientPhone, msg);
        }

        return Ok(session);
    }

    [HttpPost("{id}/simulate-response")]
    [Authorize]
    public IActionResult SimulateResponse(string id, [FromBody] SimulateResponseRequest request)
    {
        if (!_sessions.TryGetValue(id, out var session))
        {
            return NotFound(new { message = $"Triage session {id} not found." });
        }

        session.RespondedAtUtc = DateTime.UtcNow;
        session.PainScore = request.PainScore;
        session.BleedingSeverity = request.BleedingSeverity;
        session.SwellingSeverity = request.SwellingSeverity;
        session.PatientFeedback = request.PatientFeedback;

        session.EvaluateEscalation();

        return Ok(session);
    }

    [HttpPost("{id}/resolve")]
    [Authorize]
    public IActionResult ResolveTriage(string id, [FromBody] ResolveTriageRequest request)
    {
        if (!_sessions.TryGetValue(id, out var session))
        {
            return NotFound(new { message = $"Triage session {id} not found." });
        }

        var doctorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "doc-default";
        var doctorName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Dr. Attending Surgeon";

        session.IsEscalated = false;
        session.TriageStatus = "Resolved";
        session.DoctorClinicalAction = request.DoctorClinicalAction ?? "Reviewed recovery details. Patient instructed on supportive care.";
        session.ResolvedAtUtc = DateTime.UtcNow;
        session.ResolvedByDoctorId = doctorId;
        session.ResolvedByDoctorName = doctorName;

        return Ok(session);
    }
}
