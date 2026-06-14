namespace Clinic.Application.DTOs;

// ── Login ──
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
}

// ── OTP ──
public class OtpRequest
{
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

public class OtpResponse
{
    public string Message { get; set; } = string.Empty;
    public string Otp { get; set; } = string.Empty;
}

public class WhatsAppOtpRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
}

public class VerifyOtpRequest
{
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string Code { get; set; } = string.Empty;
    public bool? RemoveAfterVerification { get; set; }
}

// ── Registration ──
public class RegisterRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string Role { get; set; } = string.Empty; // "admin", "doctor", "assistant", "patient"
    public string ClinicId { get; set; } = string.Empty;
    public string? ClinicName { get; set; }
    public string? ClinicAddress { get; set; }
    public string? ClinicPhone { get; set; }
    public string? AvailabilityHours { get; set; }
    public string? AvailabilityDays { get; set; }
    public string? ClinicAvailabilityHours { get; set; }
    public string? ClinicAvailabilityDays { get; set; }
    public string? Phone { get; set; }
    public string? Gender { get; set; }
    public int? Age { get; set; }
    public string? OtpCode { get; set; }
    public string? PhoneOtpCode { get; set; }
    public string? Title { get; set; }
    public List<string>? ClinicIds { get; set; }
    public string? DoctorId { get; set; }
    public string? PatientId { get; set; }
    public string? Specialization { get; set; }
    public string? Dob { get; set; }
    public string? BloodGroup { get; set; }
    public string? Address { get; set; }
    public List<DoctorClinicAvailabilityRequest>? ClinicAvailabilities { get; set; }
}

public class DoctorClinicAvailabilityRequest
{
    public string ClinicId { get; set; } = string.Empty;
    public string AvailabilityHours { get; set; } = string.Empty;
    public List<string> AvailabilityDays { get; set; } = new();
}

// ── Social Login ──
public class SocialLoginRequest
{
    public string Provider { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string? Role { get; set; }

    // Missing data fields:
    public string? ContactNumber { get; set; }
    public string? Specialization { get; set; }
    public string? ClinicName { get; set; }
    public string? ClinicAddress { get; set; }
    public string? ClinicPhone { get; set; }
    public List<string>? ClinicIds { get; set; }
    public string? AvailabilityDays { get; set; }
    public string? AvailabilityHours { get; set; }
    public string? Gender { get; set; }
    public string? DateOfBirth { get; set; }
    public string? BloodGroup { get; set; }
    public string? Address { get; set; }
    public string? ClinicId { get; set; }
    public List<DoctorClinicAvailabilityRequest>? ClinicAvailabilities { get; set; }
}

// ── User DTO ──
public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? ClinicId { get; set; }
    public List<string>? ClinicIds { get; set; }
    public string? DoctorId { get; set; }
    public string? PatientId { get; set; }
}

// ── User Profile DTO ──
public class UserProfileDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; } // For optional updates
    public string Role { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? ClinicId { get; set; }

    // Doctor Specific fields
    public string? DoctorId { get; set; }
    public string? Specialization { get; set; }
    public string? ContactNumber { get; set; }
    public string? Avatar { get; set; }
    public string? AvailabilityDays { get; set; }
    public string? AvailabilityHours { get; set; }

    // Patient Specific fields
    public string? PatientId { get; set; }
    public string? Gender { get; set; }
    public string? DateOfBirth { get; set; }
    public string? BloodGroup { get; set; }
    public string? Address { get; set; }
    public string? Allergies { get; set; }
    public string? ChronicDiseases { get; set; }
    public string? PastIllnesses { get; set; }

    // Security verification codes
    public string? EmailOtpCode { get; set; }
    public string? PhoneOtpCode { get; set; }
}

public class ProfileOtpRequest
{
    public string? Email { get; set; }
    public string? ContactNumber { get; set; }
}
