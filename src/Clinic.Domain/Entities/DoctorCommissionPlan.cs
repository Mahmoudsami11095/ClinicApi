namespace Clinic.Domain.Entities;

public class DoctorCommissionPlan
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DoctorId { get; set; } = string.Empty;
    public string? ClinicId { get; set; }
    
    // Default base commission percentage (e.g. 30.0 for 30%)
    public decimal DefaultCommissionRate { get; set; } = 30.0m;

    // Lab fee deduction mode: "BeforeCommission", "AfterCommission", or "None"
    public string LabFeeDeductionType { get; set; } = "BeforeCommission";

    // JSON map of specialty or category overrides (e.g. {"Endodontics": 40.0, "Implantology": 35.0})
    public string SpecialtyRatesJson { get; set; } = "{}";

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Doctor? Doctor { get; set; }
    public ClinicEntity? Clinic { get; set; }
}
