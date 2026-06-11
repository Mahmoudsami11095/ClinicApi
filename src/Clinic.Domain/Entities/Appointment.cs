namespace Clinic.Domain.Entities;

public class Appointment
{
    public string Id { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // "scheduled", "completed", "cancelled"
    public string Type { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string? ClinicId { get; set; }

    // Navigation properties
    public Patient Patient { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;
    public ClinicEntity? Clinic { get; set; }
    public BillingRecord? BillingRecord { get; set; }
    public Prescription? Prescription { get; set; }
}
