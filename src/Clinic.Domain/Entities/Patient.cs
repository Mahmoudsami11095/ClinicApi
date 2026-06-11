namespace Clinic.Domain.Entities;

public class Patient
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string DateOfBirth { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string BloodGroup { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string RegistrationDate { get; set; } = string.Empty;

    // FK
    public string? ClinicId { get; set; }
    public ClinicEntity? Clinic { get; set; }

    // Navigation properties
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<BillingRecord> BillingRecords { get; set; } = new List<BillingRecord>();
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    public ICollection<DentalLog> DentalLogs { get; set; } = new List<DentalLog>();
}
