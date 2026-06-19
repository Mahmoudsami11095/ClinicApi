namespace Clinic.Domain.Entities;

public class Material
{
    public string Id { get; set; } = string.Empty;
    public string ClinicId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Unit { get; set; } // e.g. "Boxes", "Pieces", "ml"

    // Navigation properties
    public Doctor Doctor { get; set; } = null!;
    public ClinicEntity Clinic { get; set; } = null!;
}
