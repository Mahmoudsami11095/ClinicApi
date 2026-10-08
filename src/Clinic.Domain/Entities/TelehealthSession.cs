using System;

namespace Clinic.Domain.Entities;

public class TelehealthSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ClinicId { get; set; } = string.Empty;
    public string AppointmentId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string RoomToken { get; set; } = Guid.NewGuid().ToString("N");
    public string Status { get; set; } = "WaitingForPatient"; // WaitingForPatient, Active, Completed, Cancelled
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public int DurationSeconds { get; set; } = 0;
    public string? ClinicalSummary { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
