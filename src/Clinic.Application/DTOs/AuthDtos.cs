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
}

public class OtpResponse
{
    public string Message { get; set; } = string.Empty;
    public string Otp { get; set; } = string.Empty;
}

public class VerifyOtpRequest
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

// ── Registration ──
public class RegisterRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string Role { get; set; } = string.Empty; // "admin", "doctor", "assistant", "patient"
    public string ClinicId { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Gender { get; set; }
    public int? Age { get; set; }
    public string? OtpCode { get; set; }
    public string? Title { get; set; }
    public List<string>? ClinicIds { get; set; }
    public string? DoctorId { get; set; }
    public string? PatientId { get; set; }
    public string? Dob { get; set; }
    public string? BloodGroup { get; set; }
}

// ── Social Login ──
public class SocialLoginRequest
{
    public string Provider { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
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
