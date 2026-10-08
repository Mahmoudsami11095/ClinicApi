using System;
using System.ComponentModel.DataAnnotations;

namespace Clinic.Application.DTOs;

public class CreatePatientRecallDto
{
    [Required]
    public string ClinicId { get; set; } = string.Empty;

    [Required]
    public string PatientId { get; set; } = string.Empty;

    [Required]
    public string DoctorId { get; set; } = string.Empty;

    public string? SourceAppointmentId { get; set; }

    [Required]
    public string RecallType { get; set; } = "PeriodontalMaintenance"; // PeriodontalMaintenance, PediatricFluoride, ImplantCheckup, OrthodonticRetainer, PostOpFollowUp, GeneralProphylaxis

    public int RecallIntervalMonths { get; set; } = 6;

    public DateTime? CustomDueDate { get; set; }

    public string? ClinicalNotes { get; set; }
}

public class DispatchRecallDto
{
    public string Channel { get; set; } = "WhatsApp";
    public string? CustomMessage { get; set; }
}

public class SnoozeRecallDto
{
    public int SnoozeWeeks { get; set; } = 4;
    public string? Reason { get; set; }
}

public class RecallSummaryDto
{
    public int TotalDue { get; set; }
    public int OverdueCount { get; set; }
    public int DispatchedCount { get; set; }
    public int BookedCount { get; set; }
    public int CompletedCount { get; set; }
    public double ConversionRatePercentage { get; set; }
}

public class PatientRecallResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string RecallNumber { get; set; } = string.Empty;

    public string ClinicId { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;

    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string PatientPhone { get; set; } = string.Empty;

    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;

    public string? SourceAppointmentId { get; set; }
    public string? BookedAppointmentId { get; set; }

    public string RecallType { get; set; } = string.Empty;
    public int RecallIntervalMonths { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsOverdue => DueDate < DateTime.UtcNow && Status != "Completed" && Status != "Booked";

    public string Status { get; set; } = string.Empty;
    public string NotificationChannel { get; set; } = string.Empty;
    public DateTime? NotificationSentAt { get; set; }
    public int ReminderCount { get; set; }

    public DateTime? SnoozeUntilDate { get; set; }
    public string? ClinicalNotes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
