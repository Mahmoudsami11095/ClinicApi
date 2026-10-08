namespace Clinic.Application.DTOs;

public class PublicClinicBookingMetadataDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? AvailabilityHours { get; set; }
    public string? AvailabilityDays { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public bool PublicBookingEnabled { get; set; } = true;
    public string? QrPosterAssetUrl { get; set; }
    public List<PublicDoctorCardDto> Doctors { get; set; } = new();
}

public class PublicDoctorCardDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string? ClinicAvailabilityHours { get; set; }
    public List<string> ClinicAvailabilityDays { get; set; } = new();
}

public class PublicTimeSlotDto
{
    public string Time { get; set; } = string.Empty;
    public bool Available { get; set; }
}

public class PublicAppointmentBookingRequestDto
{
    public string DoctorId { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty; // "YYYY-MM-DD"
    public string Time { get; set; } = string.Empty; // "HH:mm"
    public string PatientName { get; set; } = string.Empty;
    public string PatientPhone { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? OtpCode { get; set; }
}

public class ClinicQrKitDto
{
    public string ClinicId { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string BookingUrl { get; set; } = string.Empty;
    public string QrCodeDataUrl { get; set; } = string.Empty;
}
