using System;

namespace Clinic.Domain.Entities;

public class PatientRecall
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string RecallNumber { get; set; } = string.Empty; // RCL-YYYYMM-XXXX

    public string ClinicId { get; set; } = string.Empty;
    public ClinicEntity Clinic { get; set; } = null!;

    public string PatientId { get; set; } = string.Empty;
    public Patient Patient { get; set; } = null!;

    public string DoctorId { get; set; } = string.Empty;
    public Doctor Doctor { get; set; } = null!;

    public string? SourceAppointmentId { get; set; }
    public Appointment? SourceAppointment { get; set; }

    public string? BookedAppointmentId { get; set; }
    public Appointment? BookedAppointment { get; set; }

    public string RecallType { get; set; } = "PeriodontalMaintenance"; // PeriodontalMaintenance, PediatricFluoride, ImplantCheckup, OrthodonticRetainer, PostOpFollowUp, GeneralProphylaxis
    public int RecallIntervalMonths { get; set; } = 6;
    public DateTime DueDate { get; set; }

    public string Status { get; set; } = "Scheduled"; // Scheduled, Due, NotificationSent, Confirmed, Booked, Snoozed, Completed, Cancelled
    public string NotificationChannel { get; set; } = "WhatsApp"; // WhatsApp, SMS, Email
    public DateTime? NotificationSentAt { get; set; }
    public int ReminderCount { get; set; } = 0;

    public DateTime? SnoozeUntilDate { get; set; }
    public string? ClinicalNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
}
