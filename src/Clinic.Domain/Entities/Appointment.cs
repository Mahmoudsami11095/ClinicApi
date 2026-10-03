namespace Clinic.Domain.Entities;

public class Appointment
{
    public string Id { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // "scheduled", "waiting", "in_consultation", "completed", "cancelled"
    public string Type { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string? ClinicId { get; set; }

    /// <summary>
    /// REQ-APT-02 / UAT-APT-02: Live Waiting Room Queue Management
    /// </summary>
    public string? ArrivedAt { get; set; }
    public string? ConsultationStartedAt { get; set; }
    public string? ConsultationEndedAt { get; set; }
    public int? QueueNumber { get; set; }

    // Navigation properties
    public Patient Patient { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;
    public ClinicEntity? Clinic { get; set; }
    public ICollection<BillingRecord> BillingRecords { get; set; } = new List<BillingRecord>();
    public Prescription? Prescription { get; set; }
}
