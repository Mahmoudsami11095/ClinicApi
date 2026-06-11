using Clinic.Domain.Enums;

namespace Clinic.Domain.Entities;

public class User
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string Title { get; set; } = string.Empty;

    // Optional FK – set for clinic-bound admins, assistants, patients
    public string? ClinicId { get; set; }
    public ClinicEntity? Clinic { get; set; }

    // Optional FK – links a user account to a Doctor record
    public string? DoctorId { get; set; }
    public Doctor? Doctor { get; set; }

    // Optional FK – links a user account to a Patient record
    public string? PatientId { get; set; }
    public Patient? Patient { get; set; }
}
