using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Clinic.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepo;
    private readonly IPatientRepository _patientRepo;
    private readonly IDoctorRepository _doctorRepo;
    private readonly IJwtService _jwtService;
    private readonly IOtpService _otpService;

    public AuthController(
        IUserRepository userRepo,
        IPatientRepository patientRepo,
        IDoctorRepository doctorRepo,
        IJwtService jwtService,
        IOtpService otpService)
    {
        _userRepo = userRepo;
        _patientRepo = patientRepo;
        _doctorRepo = doctorRepo;
        _jwtService = jwtService;
        _otpService = otpService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "Missing credentials" });

        var user = await _userRepo.GetByEmailAsync(request.Email);
        if (user == null)
            return Unauthorized(new { message = "Invalid credentials" });

        if (!string.IsNullOrEmpty(request.Password) &&
            !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid credentials" });

        var clinicIds = await GetDoctorClinicIds(user);
        var token = _jwtService.GenerateToken(user, clinicIds);
        var userDto = MapToUserDto(user, clinicIds);

        return Ok(new { message = "Login successful", data = userDto, token });
    }

    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] OtpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "Email required" });

        var user = await _userRepo.GetByEmailAsync(request.Email);
        if (user == null)
            return NotFound(new { message = "Email not registered" });

        var code = _otpService.GenerateOtp(request.Email);
        return Ok(new { message = "OTP sent", otp = code });
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new { message = "Email and verification code are required" });

        if (!_otpService.VerifyOtp(request.Email, request.Code))
            return Unauthorized(new { message = "Invalid verification code" });

        _otpService.RemoveOtp(request.Email);

        var user = await _userRepo.GetByEmailAsync(request.Email);
        if (user == null)
            return Unauthorized(new { message = "Invalid verification code" });

        var clinicIds = await GetDoctorClinicIds(user);
        var token = _jwtService.GenerateToken(user, clinicIds);
        var userDto = MapToUserDto(user, clinicIds);

        return Ok(new { message = "OTP verified", data = userDto, token });
    }

    [HttpPost("social")]
    public async Task<IActionResult> SocialLogin([FromBody] SocialLoginRequest request)
    {
        // Demo stub: Google → Dr. Jenkins, others → John Doe
        var email = request.Provider == "google" ? "dr.jenkins@clinic.com" : "john.doe@example.com";
        var user = await _userRepo.GetByEmailAsync(email);

        if (user == null)
            return StatusCode(500, new { message = "Social authentication failed" });

        var clinicIds = await GetDoctorClinicIds(user);
        var token = _jwtService.GenerateToken(user, clinicIds);
        var userDto = MapToUserDto(user, clinicIds);

        return Ok(new { message = $"Logged in via {request.Provider}", data = userDto, token });
    }

    [HttpPost("register-send-otp")]
    public async Task<IActionResult> RegisterSendOtp([FromBody] OtpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "Email required" });

        var existing = await _userRepo.GetByEmailAsync(request.Email);
        if (existing != null)
            return BadRequest(new { message = "Email already registered" });

        var code = _otpService.GenerateOtp(request.Email);
        return Ok(new { message = "OTP sent", otp = code });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Role) ||
            string.IsNullOrWhiteSpace(request.OtpCode))
            return BadRequest(new { message = "Missing required registration details or verification code" });

        if (!_otpService.VerifyOtp(request.Email, request.OtpCode))
            return BadRequest(new { message = "Invalid or expired verification code" });

        _otpService.RemoveOtp(request.Email);

        var existing = await _userRepo.GetByEmailAsync(request.Email);
        if (existing != null)
            return BadRequest(new { message = "Email already registered" });

        var role = Enum.Parse<UserRole>(request.Role, ignoreCase: true);

        // Generate patient ID for patient role
        string? patientId = request.PatientId;
        if (role == UserRole.Patient && string.IsNullOrEmpty(patientId))
        {
            var allPatients = await _patientRepo.GetAllAsync();
            patientId = (allPatients.Count + 1).ToString();
        }

        var newUser = new User
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name,
            Email = request.Email,
            Role = role,
            Title = role == UserRole.Patient ? "Registered Patient" :
                    role == UserRole.Doctor ? (request.Title ?? "Specialist") :
                    role == UserRole.Assistant ? "Clinical Assistant" : "Clinic Staff",
            ClinicId = string.IsNullOrWhiteSpace(request.ClinicId) ? null : request.ClinicId,
            DoctorId = request.DoctorId,
            PatientId = role == UserRole.Patient ? patientId : null,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password ?? "password123")
        };

        await _userRepo.AddAsync(newUser);

        // Create corresponding patient record
        if (role == UserRole.Patient)
        {
            var nameParts = request.Name.Split(' ', 2);
            var patient = new Patient
            {
                Id = patientId!,
                FirstName = nameParts[0],
                LastName = nameParts.Length > 1 ? nameParts[1] : "",
                Email = request.Email,
                ContactNumber = request.Phone ?? "+1234567890",
                Gender = request.Gender ?? "Male",
                DateOfBirth = request.Dob ?? "1996-01-01",
                BloodGroup = request.BloodGroup ?? "O+",
                Address = "",
                ClinicId = string.IsNullOrWhiteSpace(request.ClinicId) ? "clinic-1" : request.ClinicId,
                RegistrationDate = DateTime.UtcNow.ToString("yyyy-MM-dd")
            };
            await _patientRepo.AddAsync(patient);
        }

        var clinicIds = request.ClinicIds ??
            (string.IsNullOrEmpty(request.ClinicId) ? new List<string>() : new List<string> { request.ClinicId });
        var userDto = MapToUserDto(newUser, clinicIds);

        return Ok(new { message = "Registration successful", data = userDto });
    }

    // ── Helpers ──
    private async Task<List<string>?> GetDoctorClinicIds(User user)
    {
        if (user.Role != UserRole.Doctor || string.IsNullOrEmpty(user.DoctorId))
            return null;

        var doctor = await _doctorRepo.GetByIdAsync(user.DoctorId);
        if (doctor == null) return null;

        // Load doctor with clinics
        var doctors = await _doctorRepo.GetAllAsync(); // includes DoctorClinics
        var d = doctors.FirstOrDefault(x => x.Id == user.DoctorId);
        return d?.DoctorClinics.Select(dc => dc.ClinicId).ToList();
    }

    private static UserDto MapToUserDto(User user, List<string>? clinicIds)
    {
        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Role = user.Role.ToString().ToLower(),
            Email = user.Email,
            Title = user.Title,
            ClinicId = user.ClinicId,
            ClinicIds = clinicIds,
            DoctorId = user.DoctorId,
            PatientId = user.PatientId
        };
    }
}
