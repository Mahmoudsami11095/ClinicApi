namespace Clinic.Domain.Entities;

public class DoctorClinic
{
    public string DoctorId { get; set; } = string.Empty;
    public Doctor Doctor { get; set; } = null!;

    public string ClinicId { get; set; } = string.Empty;
    public ClinicEntity Clinic { get; set; } = null!;

    public string Status { get; set; } = "Accepted";
    
    public string? AvailabilityHours { get; set; } // e.g. "09:00-17:00"
    public string? AvailabilityDays { get; set; } // JSON array: e.g. ["Monday","Wednesday"]
}
