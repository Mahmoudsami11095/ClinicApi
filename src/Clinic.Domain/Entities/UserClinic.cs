namespace Clinic.Domain.Entities;

public class UserClinic
{
    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;

    public string ClinicId { get; set; } = string.Empty;
    public ClinicEntity Clinic { get; set; } = null!;
}
