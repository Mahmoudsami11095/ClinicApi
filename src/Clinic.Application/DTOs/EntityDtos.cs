namespace Clinic.Application.DTOs;

public class ClinicDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CreatorDoctorId { get; set; }
    public string? Status { get; set; }
}

public class PatientDto
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
    public string? ClinicId { get; set; }
    public string? Allergies { get; set; }
    public string? ChronicDiseases { get; set; }
    public string? PastIllnesses { get; set; }
}

public class DoctorAvailabilityDto
{
    public List<string> Days { get; set; } = new();
    public string Hours { get; set; } = string.Empty;
}

public class DoctorClinicAvailabilityDto
{
    public string ClinicId { get; set; } = string.Empty;
    public string AvailabilityHours { get; set; } = string.Empty;
    public List<string> AvailabilityDays { get; set; } = new();
}

public class DoctorDto
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public DoctorAvailabilityDto Availability { get; set; } = new();
    public List<string>? ClinicIds { get; set; }
    public List<DoctorClinicAvailabilityDto>? ClinicAvailabilities { get; set; }
}

public class AppointmentDto
{
    public string Id { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string? ClinicId { get; set; }
}

public class PaymentLogDto
{
    public decimal Amount { get; set; }
    public string Date { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
}

public class BillingRecordDto
{
    public string Id { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string? AppointmentId { get; set; }
    public decimal Amount { get; set; }
    public decimal? PaidAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string DateIssued { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public string? Description { get; set; }
    public string? ClinicId { get; set; }
    public List<PaymentLogDto>? Payments { get; set; }
}

public class MedicationItemDto
{
    public string Name { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
}

public class PrescriptionDto
{
    public string Id { get; set; } = string.Empty;
    public string AppointmentId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public List<MedicationItemDto> Medications { get; set; } = new();
    public string? Notes { get; set; }
}

public class DentalLogDto
{
    public string Id { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string ToothNumber { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public List<string> Status { get; set; } = new(); // Array of tooth status strings
    public int PainLevel { get; set; }
    public string? PainDetails { get; set; }
    public string? Treatment { get; set; }
    public string? Medication { get; set; }
    public bool IsPlanned { get; set; }
}
