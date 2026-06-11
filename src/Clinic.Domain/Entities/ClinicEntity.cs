namespace Clinic.Domain.Entities;

public class ClinicEntity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<DoctorClinic> DoctorClinics { get; set; } = new List<DoctorClinic>();
    public ICollection<Patient> Patients { get; set; } = new List<Patient>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<BillingRecord> BillingRecords { get; set; } = new List<BillingRecord>();
    public ICollection<User> Users { get; set; } = new List<User>();
}
