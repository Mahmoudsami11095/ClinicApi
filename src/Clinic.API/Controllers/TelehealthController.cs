using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Clinic.Application.DTOs;
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

    // Preserve existing request records for backward compatibility
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
    public IActionResult CreateSession([FromBody] CreateTelehealthSessionRequest request)
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
            InvitedSpecialistId = request.InvitedSpecialistId,
            InvitedSpecialistName = request.InvitedSpecialistName,
            InvitedSpecialty = request.InvitedSpecialty,
            Status = "WaitingForPatient",
            CreatedAtUtc = DateTime.UtcNow
        };

        session.AuditTrail.Add(new TelehealthAuditRecord
        {
            TimestampUtc = DateTime.UtcNow,
            ParticipantId = doctorId,
            ParticipantName = doctorName,
            Role = "Doctor",
            EventType = "RoomCreated",
            Details = $"Telehealth consultation room created for patient {session.PatientName}. Invited specialist: {request.InvitedSpecialistName ?? "None"}."
        });

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

    [HttpPost("sessions/{token}/start")]
    [Authorize]
    public IActionResult StartSession(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return BadRequest("Invalid token.");

        if (!_activeSessions.TryGetValue(token, out var session))
        {
            session = new TelehealthSession
            {
                RoomToken = token,
                DoctorName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Dr. Attending"
            };
            _activeSessions[token] = session;
        }

        session.StartedAtUtc = DateTime.UtcNow;
        session.Status = "Active";

        session.AuditTrail.Add(new TelehealthAuditRecord
        {
            TimestampUtc = DateTime.UtcNow,
            ParticipantId = session.DoctorId,
            ParticipantName = session.DoctorName,
            Role = "Doctor",
            EventType = "ConsultationStarted",
            Details = "WebRTC peer connection established; live consultation active."
        });

        return Ok(session);
    }

    [HttpPost("sessions/{token}/end")]
    [Authorize]
    public IActionResult EndSession(string token, [FromBody] EndTelehealthSessionRequest request)
    {
        if (string.IsNullOrWhiteSpace(token)) return BadRequest("Invalid token.");

        if (_activeSessions.TryGetValue(token, out var session))
        {
            session.EndedAtUtc = DateTime.UtcNow;
            session.Status = "Completed";
            session.ClinicalSummary = request.ClinicalSummary;
            session.DurationSeconds = request.DurationSeconds ?? 
                (session.StartedAtUtc.HasValue ? (int)(DateTime.UtcNow - session.StartedAtUtc.Value).TotalSeconds : 0);

            session.AuditTrail.Add(new TelehealthAuditRecord
            {
                TimestampUtc = DateTime.UtcNow,
                ParticipantId = session.DoctorId,
                ParticipantName = session.DoctorName,
                Role = "Doctor",
                EventType = "SessionEnded",
                Details = $"Consultation ended with clinical summary. Duration: {session.DurationSeconds}s."
            });

            // Compute cryptographic SHA-256 audit seal (BR-TELE-03)
            session.Sha256AuditSeal = ComputeSessionAuditSha256(session);
            session.IsSha256Verified = true;

            return Ok(session);
        }

        return NoContent();
    }

    // ── Release v12.0.0: Diagnostic Co-Viewer Synchronization (BR-TELE-02) ──

    [HttpPost("sessions/{token}/sync-viewport")]
    [Authorize]
    public IActionResult SyncViewport(string token, [FromBody] ViewportSyncDto request)
    {
        if (string.IsNullOrWhiteSpace(token) || request == null) return BadRequest("Invalid parameters.");

        if (!_activeSessions.TryGetValue(token, out var session))
        {
            return NotFound("Session not found.");
        }

        session.ActiveViewType = request.ViewType;
        session.LastViewportSyncJson = JsonSerializer.Serialize(request);

        session.AuditTrail.Add(new TelehealthAuditRecord
        {
            TimestampUtc = DateTime.UtcNow,
            ParticipantId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? session.DoctorId,
            ParticipantName = User.FindFirst(ClaimTypes.Name)?.Value ?? session.DoctorName,
            Role = "Doctor",
            EventType = "ViewportSync",
            Details = $"Co-viewer synchronized view: {request.ViewType}, Asset: {request.ActiveAssetId ?? "Default"}."
        });

        return Ok(new { Status = "Synchronized", request.ViewType, session.RoomToken });
    }

    // ── Release v12.0.0: Live In-Call Tele-Prescription Generator (BR-TELE-04) ──

    [HttpPost("sessions/{token}/tele-prescription")]
    [Authorize]
    public IActionResult IssueTelePrescription(string token, [FromBody] IssueTelePrescriptionRequest request)
    {
        if (string.IsNullOrWhiteSpace(token) || request == null) return BadRequest("Invalid request.");

        if (!_activeSessions.TryGetValue(token, out var session))
        {
            return NotFound("Session not found.");
        }

        var doctorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? session.DoctorId;
        var doctorName = User.FindFirst(ClaimTypes.Name)?.Value ?? session.DoctorName;

        var rxNumber = $"RX-TELE-{DateTime.UtcNow:yyyyMM}-{session.IssuedPrescriptions.Count + 1:D3}";
        var issuedAt = DateTime.UtcNow;

        var medList = request.Medications.Select(m => new TelehealthPrescriptionItemRecord
        {
            DrugName = m.DrugName,
            Dosage = m.Dosage,
            Frequency = m.Frequency,
            Duration = m.Duration,
            Instructions = m.Instructions
        }).ToList();

        var digitalSig = $"SIG-DR-{doctorId.ToUpperInvariant()}-{issuedAt.Ticks:X}";
        var sha256 = ComputePrescriptionSha256(rxNumber, doctorId, request.PatientId, issuedAt, medList.Count);
        var verificationUrl = $"/portal/prescriptions/verify?rx={rxNumber}&sig={sha256}";

        var rxRecord = new TelehealthPrescriptionRecord
        {
            RxNumber = rxNumber,
            DoctorId = doctorId,
            DoctorName = doctorName,
            PatientId = request.PatientId,
            PatientName = request.PatientName,
            IssuedAtUtc = issuedAt,
            Medications = medList,
            Notes = request.Notes,
            DigitalSignature = digitalSig,
            VerificationUrl = verificationUrl,
            DispatchChannel = request.DispatchChannel ?? "WhatsApp",
            Sha256Digest = sha256
        };

        session.IssuedPrescriptions.Add(rxRecord);

        session.AuditTrail.Add(new TelehealthAuditRecord
        {
            TimestampUtc = issuedAt,
            ParticipantId = doctorId,
            ParticipantName = doctorName,
            Role = "Doctor",
            EventType = "PrescriptionIssued",
            Details = $"E-Prescription {rxNumber} generated with {medList.Count} items. Dispatched via {rxRecord.DispatchChannel}."
        });

        return Ok(rxRecord);
    }

    [HttpGet("sessions/{token}/tele-prescriptions")]
    [Authorize]
    public IActionResult GetTelePrescriptions(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return BadRequest("Invalid token.");

        if (!_activeSessions.TryGetValue(token, out var session))
        {
            return NotFound("Session not found.");
        }

        return Ok(session.IssuedPrescriptions);
    }

    // ── Release v12.0.0: Encrypted Audit Dossier & Cryptographic Verification (BR-TELE-03) ──

    [HttpGet("sessions/{token}/audit-dossier")]
    [Authorize]
    public IActionResult GetAuditDossier(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return BadRequest("Invalid token.");

        if (!_activeSessions.TryGetValue(token, out var session))
        {
            return NotFound("Session not found.");
        }

        var dossier = new TelehealthAuditDossierDto(
            RoomToken: session.RoomToken,
            DoctorName: session.DoctorName,
            PatientName: session.PatientName,
            SpecialistName: session.InvitedSpecialistName,
            Status: session.Status,
            StartedAtUtc: session.StartedAtUtc,
            EndedAtUtc: session.EndedAtUtc,
            DurationSeconds: session.DurationSeconds,
            Sha256AuditSeal: session.Sha256AuditSeal,
            IsSha256Verified: session.IsSha256Verified,
            TotalEventsLogged: session.AuditTrail.Count,
            Events: session.AuditTrail.Select(a => new TelehealthAuditItemDto(
                a.TimestampUtc,
                a.ParticipantId,
                a.ParticipantName,
                a.Role,
                a.EventType,
                a.Details
            )).ToList(),
            Prescriptions: session.IssuedPrescriptions.Select(p => new TelehealthPrescriptionSummaryDto(
                p.Id,
                p.RxNumber,
                p.DoctorName,
                p.PatientName,
                p.IssuedAtUtc,
                p.Medications.Select(m => new TelehealthMedicationItemDto(m.DrugName, m.Dosage, m.Frequency, m.Duration, m.Instructions)).ToList(),
                p.Notes,
                p.DigitalSignature,
                p.VerificationUrl,
                p.DispatchChannel,
                p.Sha256Digest
            )).ToList()
        );

        return Ok(dossier);
    }

    [HttpPost("sessions/{token}/verify-audit")]
    [Authorize]
    public IActionResult VerifyAuditSeal(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return BadRequest("Invalid token.");

        if (!_activeSessions.TryGetValue(token, out var session))
        {
            return NotFound("Session not found.");
        }

        var calculatedSeal = session.ComputeSessionAuditSha256();
        var isValid = !string.IsNullOrEmpty(session.Sha256AuditSeal) && 
                      string.Equals(session.Sha256AuditSeal, calculatedSeal, StringComparison.OrdinalIgnoreCase);

        return Ok(new VerifyAuditSealResponse(
            RoomToken: session.RoomToken,
            CurrentSeal: session.Sha256AuditSeal,
            CalculatedSeal: calculatedSeal,
            IsValid: isValid,
            VerifiedAtUtc: DateTime.UtcNow
        ));
    }

    // ── Helper Digest Forwarders ──

    public static string ComputeSessionAuditSha256(TelehealthSession session) => session.ComputeSessionAuditSha256();

    public static string ComputePrescriptionSha256(string rxNumber, string doctorId, string patientId, DateTime issuedAt, int medCount) =>
        TelehealthSession.ComputePrescriptionSha256(rxNumber, doctorId, patientId, issuedAt, medCount);
}
