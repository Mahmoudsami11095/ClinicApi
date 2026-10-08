namespace Clinic.Application.DTOs;

public class ClinicDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CreatorDoctorId { get; set; }
    public string? Status { get; set; }
    public string? AvailabilityHours { get; set; }
    public string? AvailabilityDays { get; set; }
    public int AssistantCount { get; set; }
    
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
}

public class PatientDto
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string DateOfBirth { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public string CountryCode { get; set; } = "+20";
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string BloodGroup { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string RegistrationDate { get; set; } = string.Empty;
    public string? ClinicId { get; set; }
    public string? Allergies { get; set; }
    public string? ChronicDiseases { get; set; }
    public string? PastIllnesses { get; set; }

    // REQ-PAT-03: Patient Document & Consent E-Signatures
    public string? ConsentSignature { get; set; }
    public string? ConsentSignedAt { get; set; }
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
    public string CountryCode { get; set; } = "+20";
    public string PhoneNumber { get; set; } = string.Empty;
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

    // REQ-APT-02: Live Waiting Room Queue Management
    public string? ArrivedAt { get; set; }
    public string? ConsultationStartedAt { get; set; }
    public string? ConsultationEndedAt { get; set; }
    public int? QueueNumber { get; set; }

    // REQ-NOTIF-02: Patient Appointment Reminders
    public string? LastReminderSentAt { get; set; }
    public int? ReminderCount { get; set; } = 0;

    // REQ-CLI-03: Multi-Branch & Multi-Room Management
    public string? RoomNumber { get; set; }
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
    public string InvoiceNumber { get; set; } = string.Empty; // BR-FIN-03
    public decimal Subtotal { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? DiscountReason { get; set; }
    public string? DiscountAuthorizedBy { get; set; }
    public decimal Amount { get; set; }
    public decimal? PaidAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string DateIssued { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public string? Description { get; set; }
    public string? ClinicId { get; set; }
    public string? VoidReason { get; set; }  // BR-FIN-03
    public string? VoidedAt { get; set; }    // BR-FIN-03
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

    // BR-RX-02 Immutability & Audit Lock
    public bool IsFinalized { get; set; } = false;
    public string Status { get; set; } = "draft";
    public string? FinalizedAt { get; set; }
    public string? DigitalSignature { get; set; }
    public string? SupersedesPrescriptionId { get; set; }
    public string? SupersededById { get; set; }
    public string? SupersedeReason { get; set; }
}

public class FinalizePrescriptionDto
{
    public string? DoctorName { get; set; }
}

public class SupersedePrescriptionDto
{
    public string Reason { get; set; } = string.Empty;
    public List<MedicationItemDto> NewMedications { get; set; } = new();
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
    // BR-DEN-02: Procedure Lifecycle State Machine ("proposed", "accepted", "in_progress", "completed", "invoiced")
    public string Stage { get; set; } = "proposed";
    public decimal Cost { get; set; } = 0m;
    public string? InvoiceId { get; set; }
    public List<ConsumedMaterialDto> ConsumedMaterials { get; set; } = new();
    public string? ClinicId { get; set; }
}

public class UpdateDentalStageDto
{
    public string Stage { get; set; } = string.Empty;
}
