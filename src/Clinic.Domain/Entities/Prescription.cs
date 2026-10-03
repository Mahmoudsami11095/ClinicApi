namespace Clinic.Domain.Entities;

public class Prescription
{
    public string Id { get; set; } = string.Empty;
    public string AppointmentId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string? Notes { get; set; }

    /// <summary>
    /// BR-RX-02: Prescription Immutability & Audit Lock
    /// </summary>
    public bool IsFinalized { get; set; } = false;
    public string Status { get; set; } = "draft"; // "draft" | "finalized" | "superseded"
    public string? FinalizedAt { get; set; }
    public string? DigitalSignature { get; set; }
    public string? SupersedesPrescriptionId { get; set; }
    public string? SupersededById { get; set; }
    public string? SupersedeReason { get; set; }

    // Owned collection
    public List<MedicationItem> Medications { get; set; } = new();

    // Navigation properties
    public Appointment Appointment { get; set; } = null!;
    public Patient Patient { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;
}
