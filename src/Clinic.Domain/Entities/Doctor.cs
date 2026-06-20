namespace Clinic.Domain.Entities;

public class Doctor
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
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
    public string? Avatar { get; set; }

    // Availability stored as separate fields
    public string AvailabilityDays { get; set; } = string.Empty; // JSON array e.g. ["Monday","Wednesday"]
    public string AvailabilityHours { get; set; } = string.Empty; // e.g. "09:00-17:00"

    // Navigation properties
    public ICollection<DoctorClinic> DoctorClinics { get; set; } = new List<DoctorClinic>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    public ICollection<DentalLog> DentalLogs { get; set; } = new List<DentalLog>();
    public ICollection<RadiologyRecord> RadiologyRecords { get; set; } = new List<RadiologyRecord>();
}
