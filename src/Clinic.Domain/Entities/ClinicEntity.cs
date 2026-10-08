namespace Clinic.Domain.Entities;

public class ClinicEntity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CreatorDoctorId { get; set; }
    public string? AvailabilityHours { get; set; }
    public string? AvailabilityDays { get; set; }
    
    // Structured Location Data
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }

    // REQ-CLI-03: Multi-Branch & Multi-Room Management
    public string? BranchCode { get; set; }
    public string? Rooms { get; set; }

    // REQ-QR-01 & REQ-SAAS-01: Public Booking & Clinic QR Landing
    public string? Slug { get; set; }
    public bool PublicBookingEnabled { get; set; } = true;
    public string? QrPosterAssetUrl { get; set; }

    // Navigation properties
    public ICollection<DoctorClinic> DoctorClinics { get; set; } = new List<DoctorClinic>();
    public ICollection<Patient> Patients { get; set; } = new List<Patient>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<BillingRecord> BillingRecords { get; set; } = new List<BillingRecord>();
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<UserClinic> UserClinics { get; set; } = new List<UserClinic>();
    public ICollection<DiagnosticRequisitionOrder> DiagnosticRequisitions { get; set; } = new List<DiagnosticRequisitionOrder>();
}
