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
    private readonly IEmailService _emailService;
    private readonly ISocialAuthService _socialAuth;

    public AuthController(
        IUserRepository userRepo,
        IPatientRepository patientRepo,
        IDoctorRepository doctorRepo,
        IJwtService jwtService,
        IOtpService otpService,
        IEmailService emailService,
        ISocialAuthService socialAuth)
    {
        _userRepo = userRepo;
        _patientRepo = patientRepo;
        _doctorRepo = doctorRepo;
        _jwtService = jwtService;
        _otpService = otpService;
        _emailService = emailService;
        _socialAuth = socialAuth;
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
        await _emailService.SendEmailAsync(request.Email, "Clinic Access Code", $"Your access verification code is: <strong>{code}</strong>. It is valid for 10 minutes.");
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
        if (string.IsNullOrWhiteSpace(request.Provider) || string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(new { message = "Provider and Token are required" });

        var socialInfo = await _socialAuth.ValidateTokenAsync(request.Provider, request.Token);
        if (socialInfo == null)
            return Unauthorized(new { message = "Social authentication failed: Invalid token" });

        var user = await _userRepo.GetByEmailAsync(socialInfo.Email);

        if (user == null)
        {
            if (string.IsNullOrWhiteSpace(request.Role))
            {
                return Ok(new { requiresRoleSelection = true, email = socialInfo.Email, name = socialInfo.Name });
            }

            var isDoctor = string.Equals(request.Role, "doctor", StringComparison.OrdinalIgnoreCase);

            if (isDoctor)
            {
                var doctorId = Guid.NewGuid().ToString();

                var nameParts = socialInfo.Name.Split(' ', 2);
                var doctor = new Doctor
                {
                    Id = doctorId,
                    FirstName = nameParts[0],
                    LastName = nameParts.Length > 1 ? nameParts[1] : "",
                    Email = socialInfo.Email,
                    ContactNumber = "+1234567890",
                    Specialization = "General Medicine",
                    AvailabilityDays = "[\"Monday\",\"Tuesday\",\"Wednesday\",\"Thursday\",\"Friday\"]",
                    AvailabilityHours = "09:00-17:00"
                };

                await _doctorRepo.AddWithClinicsAsync(doctor, new List<string> { "clinic-1" });

                user = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = socialInfo.Name,
                    Email = socialInfo.Email,
                    Role = UserRole.Doctor,
                    Title = "Specialist",
                    DoctorId = doctorId,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("social-default-password-" + Guid.NewGuid().ToString())
                };
            }
            else
            {
                // Auto-register user as Patient
                var patientId = Guid.NewGuid().ToString();

                var nameParts = socialInfo.Name.Split(' ', 2);
                var patient = new Patient
                {
                    Id = patientId,
                    FirstName = nameParts[0],
                    LastName = nameParts.Length > 1 ? nameParts[1] : "",
                    Email = socialInfo.Email,
                    ContactNumber = "+1234567890",
                    Gender = "Male",
                    DateOfBirth = "1996-01-01",
                    BloodGroup = "O+",
                    Address = "",
                    ClinicId = "clinic-1",
                    RegistrationDate = DateTime.UtcNow.ToString("yyyy-MM-dd")
                };
                await _patientRepo.AddAsync(patient);

                user = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = socialInfo.Name,
                    Email = socialInfo.Email,
                    Role = UserRole.Patient,
                    Title = "Registered Patient",
                    ClinicId = "clinic-1",
                    PatientId = patientId,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("social-default-password-" + Guid.NewGuid().ToString())
                };
            }

            await _userRepo.AddAsync(user);
        }

        var clinicIds = await GetDoctorClinicIds(user);
        var appToken = _jwtService.GenerateToken(user, clinicIds);
        var userDto = MapToUserDto(user, clinicIds);

        return Ok(new { message = $"Logged in via {request.Provider}", data = userDto, token = appToken });
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
        await _emailService.SendEmailAsync(request.Email, "Clinic Registration Verification", $"Your registration verification code is: <strong>{code}</strong>. It is valid for 10 minutes.");
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
            patientId = Guid.NewGuid().ToString();
        }

        // Create corresponding doctor record if needed
        string? doctorId = request.DoctorId;
        if (role == UserRole.Doctor && string.IsNullOrEmpty(doctorId))
        {
            doctorId = Guid.NewGuid().ToString();
            var nameParts = request.Name.Split(' ', 2);
            var doctor = new Doctor
            {
                Id = doctorId,
                FirstName = nameParts[0],
                LastName = nameParts.Length > 1 ? nameParts[1] : "",
                Email = request.Email,
                ContactNumber = request.Phone ?? "+1234567890",
                Specialization = "General Medicine",
                AvailabilityDays = "[\"Monday\",\"Tuesday\",\"Wednesday\",\"Thursday\",\"Friday\"]",
                AvailabilityHours = "09:00-17:00"
            };
            var clinics = request.ClinicIds ?? 
                (string.IsNullOrEmpty(request.ClinicId) ? new List<string> { "clinic-1" } : new List<string> { request.ClinicId });
            await _doctorRepo.AddWithClinicsAsync(doctor, clinics);
        }

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
            DoctorId = role == UserRole.Doctor ? doctorId : request.DoctorId,
            PatientId = role == UserRole.Patient ? patientId : null,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password ?? "password123")
        };

        await _userRepo.AddAsync(newUser);

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
