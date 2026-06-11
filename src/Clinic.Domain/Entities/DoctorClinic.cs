namespace Clinic.Domain.Entities;

public class DoctorClinic
{
    public string DoctorId { get; set; } = string.Empty;
    public Doctor Doctor { get; set; } = null!;

    public string ClinicId { get; set; } = string.Empty;
    public ClinicEntity Clinic { get; set; } = null!;
}
