namespace Clinic.Domain.Entities;

public class Patient
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string DateOfBirth { get; set; } = string.Empty;
    public string CountryCode { get; set; } = "+20";
    public string PhoneNumber { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string ContactNumber
    {
        get => $"{CountryCode} {PhoneNumber}".Trim();
        set
        {
            var split = Helpers.PhoneHelper.SplitContactNumber(value);
            CountryCode = split.CountryCode;
            PhoneNumber = split.PhoneNumber;
        }
    }
    public string Email { get; set; } = string.Empty;
    public string BloodGroup { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string RegistrationDate { get; set; } = string.Empty;
    public string? Allergies { get; set; }
    public string? ChronicDiseases { get; set; }
    public string? PastIllnesses { get; set; }

    // FK
    public string? ClinicId { get; set; }
    public ClinicEntity? Clinic { get; set; }

    // Navigation properties
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<BillingRecord> BillingRecords { get; set; } = new List<BillingRecord>();
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    public ICollection<DentalLog> DentalLogs { get; set; } = new List<DentalLog>();
    public ICollection<RadiologyRecord> RadiologyRecords { get; set; } = new List<RadiologyRecord>();
}
