namespace Clinic.Domain.Entities;

public class RadiologyRecord
{
    public string Id { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string RadiologyCenterId { get; set; } = string.Empty;
    public string ProcedureName { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
    public string Date { get; set; } = string.Empty;
    public string? Notes { get; set; }

    // Navigation properties
    public Doctor Doctor { get; set; } = null!;
    public Patient Patient { get; set; } = null!;
    public RadiologyCenter RadiologyCenter { get; set; } = null!;
}
