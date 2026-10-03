namespace Clinic.Domain.Entities;

public class ClinicalNote
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PatientId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string? ClinicId { get; set; }
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");
    public string Title { get; set; } = "Clinical Note";
    public string Category { get; set; } = "Consultation";
    
    /// <summary>
    /// BR-RX-03 / BR-MED-01: Immutable original clinical encounter note text.
    /// This property is append-only on creation and never overwritten.
    /// </summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Chronological list of timestamped amendments preserving author identity.
    /// </summary>
    public List<ClinicalNoteAmendment> Amendments { get; set; } = new();

    // Navigation properties
    public Patient Patient { get; set; } = null!;
    public ClinicEntity? Clinic { get; set; }
}
