using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Clinic.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepo;
    private readonly IPatientRepository _patientRepo;
    private readonly IDoctorRepository _doctorRepo;
    private readonly IClinicRepository _clinicRepo;
    private readonly IJwtService _jwtService;
    private readonly IOtpService _otpService;
    private readonly IEmailService _emailService;
    private readonly ISocialAuthService _socialAuth;
    private readonly IWhatsAppOtpService _whatsappOtpService;

    public AuthController(
        IUserRepository userRepo,
        IPatientRepository patientRepo,
        IDoctorRepository doctorRepo,
        IClinicRepository clinicRepo,
        IJwtService jwtService,
        IOtpService otpService,
        IEmailService emailService,
        ISocialAuthService socialAuth,
        IWhatsAppOtpService whatsappOtpService)
    {
        _userRepo = userRepo;
        _patientRepo = patientRepo;
        _doctorRepo = doctorRepo;
        _clinicRepo = clinicRepo;
        _jwtService = jwtService;
        _otpService = otpService;
        _emailService = emailService;
        _socialAuth = socialAuth;
        _whatsappOtpService = whatsappOtpService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "Missing credentials" });

        var user = await _userRepo.GetByEmailAsync(request.Email);
        if (user == null)
        {
            user = await _userRepo.GetByPhoneNumberAsync(request.Email);
        }

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
            return BadRequest(new { message = "Email or Phone Number required" });

        bool isEmail = request.Email.Contains("@");

        if (isEmail)
        {
            var user = await _userRepo.GetByEmailAsync(request.Email);
            if (user == null)
                return NotFound(new { message = "Email not registered" });

            var code = _otpService.GenerateOtp(request.Email);
            await _emailService.SendEmailAsync(request.Email, "Clinic Access Code", $"Your access verification code is: <strong>{code}</strong>. It is valid for 10 minutes.");
            return Ok(new { message = "OTP sent", otp = code });
        }
        else
        {
            var user = await _userRepo.GetByPhoneNumberAsync(request.Email);
            if (user == null)
                return NotFound(new { message = "Phone number not registered" });

            var (success, message, whatsappCode) = await _whatsappOtpService.RequestOtpAsync(request.Email);
            if (!success)
            {
                if (message.Contains("Too many OTP requests", StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(429, new { message });
                }
                return BadRequest(new { message });
            }

            return Ok(new { message = "OTP sent via WhatsApp successfully", otp = whatsappCode });
        }
    }

    [HttpPost("request-otp")]
    public async Task<IActionResult> RequestOtp([FromBody] WhatsAppOtpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            return BadRequest(new { message = "Phone number is required." });

        var (success, message, code) = await _whatsappOtpService.RequestOtpAsync(request.PhoneNumber);
        if (!success)
        {
            if (message.Contains("Too many OTP requests", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(429, new { message });
            }
            return BadRequest(new { message });
        }

        return Ok(new { message = "OTP sent via WhatsApp successfully.", otp = code });
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new { message = "Verification code is required" });

        // WhatsApp OTP Flow (Explicit Phone Number)
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            if (!_whatsappOtpService.VerifyOtp(request.PhoneNumber, request.Code))
                return Unauthorized(new { message = "Invalid or expired verification code" });

            _whatsappOtpService.RemoveOtp(request.PhoneNumber);

            var user = await _userRepo.GetByPhoneNumberAsync(request.PhoneNumber);
            if (user == null)
            {
                return Ok(new { message = "Phone number verified successfully" });
            }

            var clinicIds = await GetDoctorClinicIds(user);
            var token = _jwtService.GenerateToken(user, clinicIds);
            var userDto = MapToUserDto(user, clinicIds);

            return Ok(new { message = "OTP verified", data = userDto, token });
        }

        // Email OTP Flow or Phone Number submitted as 'Email' field
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "Email or Phone Number is required" });

        bool isEmail = request.Email.Contains("@");

        if (!isEmail)
        {
            if (!_whatsappOtpService.VerifyOtp(request.Email, request.Code))
                return Unauthorized(new { message = "Invalid or expired verification code" });

            _whatsappOtpService.RemoveOtp(request.Email);

            var user = await _userRepo.GetByPhoneNumberAsync(request.Email);
            if (user == null)
                return Unauthorized(new { message = "Invalid verification code" });

            var clinicIds = await GetDoctorClinicIds(user);
            var token = _jwtService.GenerateToken(user, clinicIds);
            var userDto = MapToUserDto(user, clinicIds);

            return Ok(new { message = "OTP verified", data = userDto, token });
        }

        // Email verification
        if (!_otpService.VerifyOtp(request.Email, request.Code))
            return Unauthorized(new { message = "Invalid verification code" });

        _otpService.RemoveOtp(request.Email);

        var emailUser = await _userRepo.GetByEmailAsync(request.Email);
        if (emailUser == null)
            return Unauthorized(new { message = "Invalid verification code" });

        var emailClinicIds = await GetDoctorClinicIds(emailUser);
        var emailToken = _jwtService.GenerateToken(emailUser, emailClinicIds);
        var emailUserDto = MapToUserDto(emailUser, emailClinicIds);

        return Ok(new { message = "OTP verified", data = emailUserDto, token = emailToken });
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
                    ContactNumber = request.ContactNumber ?? "+1234567890",
                    Specialization = request.Specialization ?? "General Medicine",
                    AvailabilityDays = request.AvailabilityDays ?? "[\"Monday\",\"Tuesday\",\"Wednesday\",\"Thursday\",\"Friday\"]",
                    AvailabilityHours = request.AvailabilityHours ?? "09:00-17:00"
                };

                var doctorClinics = new List<DoctorClinic>();
                if (!string.IsNullOrEmpty(request.ClinicName))
                {
                    var newClinic = new ClinicEntity
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = request.ClinicName,
                        Address = request.ClinicAddress ?? "Primary Address",
                        Phone = request.ClinicPhone ?? request.ContactNumber ?? "+1234567890",
                        CreatorDoctorId = doctorId,
                        AvailabilityHours = request.AvailabilityHours ?? "09:00-17:00",
                        AvailabilityDays = request.AvailabilityDays ?? "[\"Monday\",\"Tuesday\",\"Wednesday\",\"Thursday\",\"Friday\"]"
                    };
                    await _clinicRepo.AddAsync(newClinic);

                    doctorClinics.Add(new DoctorClinic
                    {
                        DoctorId = doctorId,
                        ClinicId = newClinic.Id,
                        AvailabilityHours = request.AvailabilityHours ?? "09:00-17:00",
                        AvailabilityDays = request.AvailabilityDays ?? "[\"Monday\",\"Tuesday\",\"Wednesday\",\"Thursday\",\"Friday\"]",
                        Status = "Accepted"
                    });
                }
                else if (request.ClinicAvailabilities != null && request.ClinicAvailabilities.Any())
                {
                    foreach (var ca in request.ClinicAvailabilities)
                    {
                        doctorClinics.Add(new DoctorClinic
                        {
                            DoctorId = doctorId,
                            ClinicId = ca.ClinicId,
                            AvailabilityHours = ca.AvailabilityHours,
                            AvailabilityDays = System.Text.Json.JsonSerializer.Serialize(ca.AvailabilityDays),
                            Status = "Accepted"
                        });
                    }
                }
                doctor.DoctorClinics = doctorClinics;
                await _doctorRepo.AddAsync(doctor);

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
                    ContactNumber = request.ContactNumber ?? "+1234567890",
                    Gender = request.Gender ?? "Male",
                    DateOfBirth = request.DateOfBirth ?? "1996-01-01",
                    BloodGroup = request.BloodGroup ?? "O+",
                    Address = request.Address ?? "",
                    ClinicId = string.IsNullOrEmpty(request.ClinicId) ? "clinic-1" : request.ClinicId,
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
                    ClinicId = string.IsNullOrEmpty(request.ClinicId) ? "clinic-1" : request.ClinicId,
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

        var existingEmail = await _userRepo.GetByEmailAsync(request.Email);
        if (existingEmail != null)
            return BadRequest(new { message = "Email already registered" });

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var existingPhone = await _userRepo.GetByPhoneNumberAsync(request.Phone);
            if (existingPhone != null)
                return BadRequest(new { message = "Phone number already registered" });

            // Generate and send Email OTP
            var emailCode = _otpService.GenerateOtp(request.Email);
            await _emailService.SendEmailAsync(request.Email, "Clinic Registration Verification", $"Your registration verification code is: <strong>{emailCode}</strong>. It is valid for 10 minutes.");

            // Generate and send WhatsApp OTP
            var (success, message, whatsappCode) = await _whatsappOtpService.RequestOtpAsync(request.Phone);
            if (!success)
            {
                if (message.Contains("Too many OTP requests", StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(429, new { message });
                }
                return BadRequest(new { message });
            }

            return Ok(new { message = "OTPs sent to email and WhatsApp successfully", emailOtp = emailCode, whatsappOtp = whatsappCode, otp = emailCode });
        }
        else
        {
            // Only Email OTP
            var emailCode = _otpService.GenerateOtp(request.Email);
            await _emailService.SendEmailAsync(request.Email, "Clinic Registration Verification", $"Your registration verification code is: <strong>{emailCode}</strong>. It is valid for 10 minutes.");
            return Ok(new { message = "OTP sent to email", emailOtp = emailCode, otp = emailCode });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Role))
            return BadRequest(new { message = "Missing required registration details" });

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            if (string.IsNullOrWhiteSpace(request.OtpCode) || string.IsNullOrWhiteSpace(request.PhoneOtpCode))
                return BadRequest(new { message = "Both Email verification code and WhatsApp verification code are required" });

            if (!_otpService.VerifyOtp(request.Email, request.OtpCode))
                return BadRequest(new { message = "Invalid or expired Email verification code" });

            if (!_whatsappOtpService.VerifyOtp(request.Phone, request.PhoneOtpCode))
                return BadRequest(new { message = "Invalid or expired WhatsApp verification code" });

            _otpService.RemoveOtp(request.Email);
            _whatsappOtpService.RemoveOtp(request.Phone);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.OtpCode))
                return BadRequest(new { message = "Email verification code is required" });

            if (!_otpService.VerifyOtp(request.Email, request.OtpCode))
                return BadRequest(new { message = "Invalid or expired Email verification code" });

            _otpService.RemoveOtp(request.Email);
        }

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

        List<string>? registeredClinicIds = request.ClinicIds;

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
                Specialization = request.Specialization ?? "General Medicine",
                AvailabilityDays = "[\"Monday\",\"Tuesday\",\"Wednesday\",\"Thursday\",\"Friday\"]",
                AvailabilityHours = "09:00-17:00"
            };
            var clinics = request.ClinicIds ?? new List<string>();
            if (clinics.Count == 0)
            {
                if (!string.IsNullOrEmpty(request.ClinicName))
                {
                    var newClinic = new ClinicEntity
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = request.ClinicName,
                        Address = request.ClinicAddress ?? "Primary Address",
                        Phone = request.ClinicPhone ?? request.Phone ?? "+1234567890",
                        CreatorDoctorId = doctorId,
                        AvailabilityHours = request.ClinicAvailabilityHours ?? request.AvailabilityHours ?? "09:00-17:00",
                        AvailabilityDays = request.ClinicAvailabilityDays ?? request.AvailabilityDays ?? "[\"Monday\",\"Tuesday\",\"Wednesday\",\"Thursday\",\"Friday\"]"
                    };
                    await _clinicRepo.AddAsync(newClinic);
                    clinics.Add(newClinic.Id);
                }
                else if (!string.IsNullOrEmpty(request.ClinicId))
                {
                    clinics.Add(request.ClinicId);
                }
                else
                {
                    clinics.Add("clinic-1");
                }
            }

            var doctorClinics = new List<DoctorClinic>();
            if (request.ClinicAvailabilities != null && request.ClinicAvailabilities.Any())
            {
                foreach (var ca in request.ClinicAvailabilities)
                {
                    doctorClinics.Add(new DoctorClinic
                    {
                        DoctorId = doctorId,
                        ClinicId = ca.ClinicId,
                        AvailabilityHours = ca.AvailabilityHours,
                        AvailabilityDays = System.Text.Json.JsonSerializer.Serialize(ca.AvailabilityDays),
                        Status = "Accepted"
                    });
                }
                registeredClinicIds = request.ClinicAvailabilities.Select(ca => ca.ClinicId).ToList();
            }
            else
            {
                foreach (var c in clinics)
                {
                    doctorClinics.Add(new DoctorClinic
                    {
                        DoctorId = doctorId,
                        ClinicId = c,
                        AvailabilityHours = "09:00-17:00",
                        AvailabilityDays = "[\"Monday\",\"Tuesday\",\"Wednesday\",\"Thursday\",\"Friday\"]",
                        Status = "Accepted"
                    });
                }
                registeredClinicIds = clinics;
            }
            doctor.DoctorClinics = doctorClinics;
            await _doctorRepo.AddAsync(doctor);
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
                Address = request.Address ?? "",
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

        var clinicIds = registeredClinicIds ??
            (string.IsNullOrEmpty(request.ClinicId) ? new List<string>() : new List<string> { request.ClinicId });
        var userDto = MapToUserDto(newUser, clinicIds);

        return Ok(new { message = "Registration successful", data = userDto });
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userRepo.GetAllAsync();
        var dtos = new List<UserDto>();
        foreach (var u in users)
        {
            var clinicIds = await GetDoctorClinicIds(u);
            dtos.Add(MapToUserDto(u, clinicIds));
        }
        return Ok(new { data = dtos });
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        var profile = new UserProfileDto
        {
            Name = user.Name,
            Email = user.Email,
            Role = user.Role.ToString().ToLower(),
            Title = user.Title ?? "",
            ClinicId = user.ClinicId,
            DoctorId = user.DoctorId,
            PatientId = user.PatientId
        };

        if (user.Role == UserRole.Doctor && !string.IsNullOrEmpty(user.DoctorId))
        {
            var doctor = await _doctorRepo.GetByIdAsync(user.DoctorId);
            if (doctor != null)
            {
                profile.Specialization = doctor.Specialization;
                profile.ContactNumber = doctor.ContactNumber;
                profile.Avatar = doctor.Avatar;
                profile.AvailabilityDays = doctor.AvailabilityDays;
                profile.AvailabilityHours = doctor.AvailabilityHours;
            }
        }
        else if (user.Role == UserRole.Patient && !string.IsNullOrEmpty(user.PatientId))
        {
            var patient = await _patientRepo.GetByIdAsync(user.PatientId);
            if (patient != null)
            {
                profile.Gender = patient.Gender;
                profile.DateOfBirth = patient.DateOfBirth;
                profile.BloodGroup = patient.BloodGroup;
                profile.Address = patient.Address;
                profile.ContactNumber = patient.ContactNumber;
                profile.Allergies = patient.Allergies;
                profile.ChronicDiseases = patient.ChronicDiseases;
                profile.PastIllnesses = patient.PastIllnesses;
            }
        }

        return Ok(new { data = profile });
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UserProfileDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _userRepo.GetByEmailAsync(dto.Email);
            if (existing != null)
                return BadRequest(new { message = "Email is already in use by another account" });
        }

        user.Name = dto.Name;
        user.Email = dto.Email;

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        }

        await _userRepo.UpdateAsync(user);

        if (user.Role == UserRole.Doctor && !string.IsNullOrEmpty(user.DoctorId))
        {
            var doctor = await _doctorRepo.GetByIdAsync(user.DoctorId);
            if (doctor != null)
            {
                var nameParts = dto.Name.Split(' ', 2);
                doctor.FirstName = nameParts[0];
                doctor.LastName = nameParts.Length > 1 ? nameParts[1] : "";
                doctor.Email = dto.Email;
                doctor.Specialization = dto.Specialization ?? doctor.Specialization;
                doctor.ContactNumber = dto.ContactNumber ?? doctor.ContactNumber;
                doctor.Avatar = dto.Avatar ?? doctor.Avatar;
                doctor.AvailabilityDays = dto.AvailabilityDays ?? doctor.AvailabilityDays;
                doctor.AvailabilityHours = dto.AvailabilityHours ?? doctor.AvailabilityHours;
                await _doctorRepo.UpdateAsync(doctor);
            }
        }
        else if (user.Role == UserRole.Patient && !string.IsNullOrEmpty(user.PatientId))
        {
            var patient = await _patientRepo.GetByIdAsync(user.PatientId);
            if (patient != null)
            {
                var nameParts = dto.Name.Split(' ', 2);
                patient.FirstName = nameParts[0];
                patient.LastName = nameParts.Length > 1 ? nameParts[1] : "";
                patient.Email = dto.Email;
                patient.Gender = dto.Gender ?? patient.Gender;
                patient.DateOfBirth = dto.DateOfBirth ?? patient.DateOfBirth;
                patient.BloodGroup = dto.BloodGroup ?? patient.BloodGroup;
                patient.Address = dto.Address ?? patient.Address;
                patient.ContactNumber = dto.ContactNumber ?? patient.ContactNumber;
                patient.Allergies = dto.Allergies ?? patient.Allergies;
                patient.ChronicDiseases = dto.ChronicDiseases ?? patient.ChronicDiseases;
                patient.PastIllnesses = dto.PastIllnesses ?? patient.PastIllnesses;
                await _patientRepo.UpdateAsync(patient);
            }
        }

        return Ok(new { message = "Profile updated successfully", data = dto });
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
