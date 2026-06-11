namespace Clinic.Domain.Entities;

public class DentalLog
{
    public string Id { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string ToothNumber { get; set; } = string.Empty; // 1-32 or A-T
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // JSON array of ToothStatus values
    public int PainLevel { get; set; }
    public string? PainDetails { get; set; }
    public string? Treatment { get; set; }
    public string? Medication { get; set; }

    // Navigation properties
    public Patient Patient { get; set; } = null!;
}
