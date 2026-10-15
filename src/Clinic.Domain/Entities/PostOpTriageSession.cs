using System;

namespace Clinic.Domain.Entities;

public class PostOpTriageSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ClinicId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string PatientPhone { get; set; } = string.Empty;
    public string ProcedureName { get; set; } = string.Empty;
    public DateTime ProcedureDate { get; set; } = DateTime.UtcNow.Date;
    public int IntervalHours { get; set; } = 24; // 24h, 48h check-in
    public string TriageStatus { get; set; } = "Scheduled"; // Scheduled, PendingResponse, Recovering, Escalated, Resolved
    public DateTime ScheduledDispatchUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DispatchedAtUtc { get; set; }
    public DateTime? RespondedAtUtc { get; set; }
    public int? PainScore { get; set; } // 1 - 10
    public string? BleedingSeverity { get; set; } // None, Slight, Severe
    public string? SwellingSeverity { get; set; } // None, Moderate, Severe
    public string? PatientFeedback { get; set; }
    public bool IsEscalated { get; set; } = false;
    public string? EscalationReason { get; set; }
    public string? DoctorClinicalAction { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public string? ResolvedByDoctorId { get; set; }
    public string? ResolvedByDoctorName { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public void EvaluateEscalation()
    {
        var reasons = new System.Collections.Generic.List<string>();

        if (PainScore.HasValue && PainScore.Value > 7)
        {
            reasons.Add($"Severe pain level reported ({PainScore.Value}/10)");
        }

        if (string.Equals(BleedingSeverity, "Severe", StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add("Persistent severe active bleeding");
        }

        if (string.Equals(SwellingSeverity, "Severe", StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add("Significant acute facial/intraoral swelling");
        }

        if (reasons.Count > 0)
        {
            IsEscalated = true;
            TriageStatus = "Escalated";
            EscalationReason = string.Join("; ", reasons);
        }
        else
        {
            IsEscalated = false;
            TriageStatus = "Recovering";
            EscalationReason = null;
        }
    }
}
