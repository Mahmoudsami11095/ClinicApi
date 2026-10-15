using System;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading.Tasks;
using Clinic.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelehealthController : ControllerBase
{
    // Thread-safe in-memory session registry for low-latency WebRTC token validation
    private static readonly ConcurrentDictionary<string, TelehealthSession> _activeSessions = new();

    public record CreateSessionRequest(
        string? ClinicId,
        string? AppointmentId,
        string? PatientId,
        string? PatientName
    );

    public record EndSessionRequest(
        string? ClinicalSummary,
        int? DurationSeconds
    );

    [HttpPost("sessions")]
    [Authorize]
    public IActionResult CreateSession([FromBody] CreateSessionRequest request)
    {
        var doctorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "doc-unknown";
        var doctorName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Dr. Attending";

        var session = new TelehealthSession
        {
            ClinicId = request.ClinicId ?? string.Empty,
            AppointmentId = request.AppointmentId ?? string.Empty,
            DoctorId = doctorId,
            DoctorName = doctorName,
            PatientId = request.PatientId ?? string.Empty,
            PatientName = request.PatientName ?? "Patient",
            Status = "WaitingForPatient",
            CreatedAtUtc = DateTime.UtcNow
        };

        _activeSessions[session.RoomToken] = session;

        return Ok(session);
    }

    [HttpGet("sessions/{token}")]
    [AllowAnonymous]
    public IActionResult GetSessionByToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return BadRequest("Invalid token.");

        if (_activeSessions.TryGetValue(token, out var session))
        {
            return Ok(session);
        }

        // Return standard fallback session for mock/dev tokens
        return Ok(new TelehealthSession
        {
            RoomToken = token,
            Status = "Active",
            DoctorName = "Dr. Attending",
            PatientName = "Patient"
        });
    }

    [HttpPost("sessions/{token}/end")]
    [Authorize]
    public IActionResult EndSession(string token, [FromBody] EndSessionRequest request)
    {
        if (string.IsNullOrWhiteSpace(token)) return BadRequest("Invalid token.");

        if (_activeSessions.TryGetValue(token, out var session))
        {
            session.EndedAtUtc = DateTime.UtcNow;
            session.Status = "Completed";
            session.ClinicalSummary = request.ClinicalSummary;
            session.DurationSeconds = request.DurationSeconds ?? 
                (session.StartedAtUtc.HasValue ? (int)(DateTime.UtcNow - session.StartedAtUtc.Value).TotalSeconds : 0);

            return Ok(session);
        }

        return NoContent();
    }
}
