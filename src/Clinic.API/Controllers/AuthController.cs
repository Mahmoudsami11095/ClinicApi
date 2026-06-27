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
        {
            bool isEmail = request.Email.Contains("@");
            return NotFound(new { message = isEmail ? "No account found with this email address" : "No account found with this phone number" });
        }

        if (!string.IsNullOrEmpty(request.Password) &&
            !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Incorrect password" });

        var clinicIds = await GetUserClinicIds(user);
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

        bool remove = request.RemoveAfterVerification ?? true;

        // WhatsApp OTP Flow (Explicit Phone Number)
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            if (!_whatsappOtpService.VerifyOtp(request.PhoneNumber, request.Code))
                return Unauthorized(new { message = "Invalid or expired verification code" });

            if (remove)
                _whatsappOtpService.RemoveOtp(request.PhoneNumber);

            var user = await _userRepo.GetByPhoneNumberAsync(request.PhoneNumber);
            if (user == null)
            {
                return Ok(new { message = "Phone number verified successfully" });
            }

            var clinicIds = await GetUserClinicIds(user);
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

            if (remove)
                _whatsappOtpService.RemoveOtp(request.Email);

            var user = await _userRepo.GetByPhoneNumberAsync(request.Email);
            if (user == null)
                return Ok(new { message = "Phone number verified successfully" });

            var clinicIds = await GetUserClinicIds(user);
            var token = _jwtService.GenerateToken(user, clinicIds);
            var userDto = MapToUserDto(user, clinicIds);

            return Ok(new { message = "OTP verified", data = userDto, token });
        }

        // Email verification
        if (!_otpService.VerifyOtp(request.Email, request.Code))
            return Unauthorized(new { message = "Invalid verification code" });

        if (remove)
            _otpService.RemoveOtp(request.Email);

        var emailUser = await _userRepo.GetByEmailAsync(request.Email);
        if (emailUser == null)
            return Ok(new { message = "Email verified successfully" });

        var emailClinicIds = await GetUserClinicIds(emailUser);
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
            var isAssistant = string.Equals(request.Role, "assistant", StringComparison.OrdinalIgnoreCase);

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
                    SpecializationId = request.SpecializationId,
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
                        AvailabilityHours = request.ClinicAvailabilityHours ?? request.AvailabilityHours ?? "09:00-17:00",
                        AvailabilityDays = request.ClinicAvailabilityDays ?? request.AvailabilityDays ?? "[\"Monday\",\"Tuesday\",\"Wednesday\",\"Thursday\",\"Friday\"]",
                        Latitude = request.Latitude,
                        Longitude = request.Longitude,
                        City = request.City,
                        State = request.State,
                        Country = request.Country
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
                    Title = request.Title ?? "Specialist",
                    DoctorId = doctorId,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(!string.IsNullOrWhiteSpace(request.Password) ? request.Password : ("social-default-password-" + Guid.NewGuid().ToString()))
                };
            }
            else if (isAssistant)
            {
                user = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = socialInfo.Name,
                    Email = socialInfo.Email,
                    Role = UserRole.Assistant,
                    Title = "Clinical Assistant",
                    ClinicId = string.IsNullOrEmpty(request.ClinicId) ? null : request.ClinicId,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(!string.IsNullOrWhiteSpace(request.Password) ? request.Password : ("social-default-password-" + Guid.NewGuid().ToString()))
                };

                var clinics = request.ClinicIds ?? new List<string>();
                if (!string.IsNullOrWhiteSpace(request.ClinicId) && !clinics.Contains(request.ClinicId))
                {
                    clinics.Add(request.ClinicId);
                }
                user.UserClinics = clinics.Select(cid => new UserClinic { ClinicId = cid, UserId = user.Id }).ToList();
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
                    Address = request.Address ?? "",
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    City = request.City,
                    State = request.State,
                    Country = request.Country,
                    Gender = request.Gender ?? "Not Specified",
                    DateOfBirth = request.DateOfBirth ?? "1996-01-01",
                    BloodGroup = request.BloodGroup ?? "O+",
                    ClinicId = string.IsNullOrEmpty(request.ClinicId) ? null : request.ClinicId,
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
                    ClinicId = string.IsNullOrEmpty(request.ClinicId) ? null : request.ClinicId,
                    PatientId = patientId,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(!string.IsNullOrWhiteSpace(request.Password) ? request.Password : ("social-default-password-" + Guid.NewGuid().ToString()))
                };
            }

            await _userRepo.AddAsync(user);
        }

        var clinicIds = await GetUserClinicIds(user);
        var appToken = _jwtService.GenerateToken(user, clinicIds);
        var userDto = MapToUserDto(user, clinicIds);

        return Ok(new { message = $"Logged in via {request.Provider}", data = userDto, token = appToken });
    }

    [HttpPost("check-availability")]
    public async Task<IActionResult> CheckAvailability([FromBody] CheckAvailabilityRequest request)
    {
        var errors = new List<string>();

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var existingEmail = await _userRepo.GetByEmailAsync(request.Email);
            if (existingEmail != null)
                errors.Add("Email is already registered.");
        }

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var split = Clinic.Domain.Helpers.PhoneHelper.SplitContactNumber(request.Phone);
            var validation = Clinic.Domain.Helpers.PhoneHelper.ValidatePhoneNumber(split.CountryCode, split.PhoneNumber);
            if (!validation.IsValid)
            {
                errors.Add(validation.ErrorMessage);
            }
            else
            {
                var normPhone = Clinic.Domain.Helpers.PhoneHelper.NormalizePhoneNumber(split.CountryCode, split.PhoneNumber);
                var isUnique = await _userRepo.IsPhoneNumberUniqueAsync(split.CountryCode, normPhone);
                if (!isUnique)
                    errors.Add("Phone number is already registered.");

                var existingPhone = await _userRepo.GetByPhoneNumberAsync(request.Phone);
                if (existingPhone != null && !errors.Contains("Phone number is already registered."))
                    errors.Add("Phone number is already registered.");
            }
        }

        if (errors.Count > 0)
            return BadRequest(new { message = string.Join(" ", errors), errors });

        return Ok(new { message = "Available" });
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
            var split = Clinic.Domain.Helpers.PhoneHelper.SplitContactNumber(request.Phone);
            var validation = Clinic.Domain.Helpers.PhoneHelper.ValidatePhoneNumber(split.CountryCode, split.PhoneNumber);
            if (!validation.IsValid)
                return BadRequest(new { message = validation.ErrorMessage });

            var normPhone = Clinic.Domain.Helpers.PhoneHelper.NormalizePhoneNumber(split.CountryCode, split.PhoneNumber);
            var isUnique = await _userRepo.IsPhoneNumberUniqueAsync(split.CountryCode, normPhone);
            if (!isUnique)
                return BadRequest(new { message = "Phone number already registered to another account." });

            var existingPhone = await _userRepo.GetByPhoneNumberAsync(request.Phone);
            if (existingPhone != null)
                return BadRequest(new { message = "Phone number already registered to another account." });

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

            return Ok(new { message = "OTPs sent to email and WhatsApp successfully" });
        }
        else
        {
            // Only Email OTP
            var emailCode = _otpService.GenerateOtp(request.Email);
            await _emailService.SendEmailAsync(request.Email, "Clinic Registration Verification", $"Your registration verification code is: <strong>{emailCode}</strong>. It is valid for 10 minutes.");
            return Ok(new { message = "OTP sent to email" });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Role))
            return BadRequest(new { message = "Missing required registration details" });

        // Email OTP is always required
        if (string.IsNullOrWhiteSpace(request.OtpCode))
            return BadRequest(new { message = "Email verification code is required" });

        if (!_otpService.VerifyOtp(request.Email, request.OtpCode))
            return BadRequest(new { message = "Invalid or expired Email verification code" });

        _otpService.RemoveOtp(request.Email);

        // WhatsApp OTP is optional – only verify if the user provided it
        if (!string.IsNullOrWhiteSpace(request.Phone) && !string.IsNullOrWhiteSpace(request.PhoneOtpCode))
        {
            if (!_whatsappOtpService.VerifyOtp(request.Phone, request.PhoneOtpCode))
                return BadRequest(new { message = "Invalid or expired WhatsApp verification code" });

            _whatsappOtpService.RemoveOtp(request.Phone);
        }

        var existing = await _userRepo.GetByEmailAsync(request.Email);
        if (existing != null)
            return BadRequest(new { message = "Email already registered" });

        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
            return BadRequest(new { message = $"Invalid role '{request.Role}'. Must be one of: doctor, patient, assistant." });

        var countryCode = request.CountryCode;
        var phoneNumber = request.PhoneNumber;

        if (string.IsNullOrEmpty(phoneNumber) && !string.IsNullOrEmpty(request.Phone))
        {
            var split = Clinic.Domain.Helpers.PhoneHelper.SplitContactNumber(request.Phone);
            countryCode = split.CountryCode;
            phoneNumber = split.PhoneNumber;
        }

        if (!string.IsNullOrEmpty(phoneNumber))
        {
            if (string.IsNullOrEmpty(countryCode))
            {
                countryCode = "+20";
            }

            var validation = Clinic.Domain.Helpers.PhoneHelper.ValidatePhoneNumber(countryCode, phoneNumber);
            if (!validation.IsValid)
                return BadRequest(new { message = validation.ErrorMessage });

            phoneNumber = Clinic.Domain.Helpers.PhoneHelper.NormalizePhoneNumber(countryCode, phoneNumber);

            var isUnique = await _userRepo.IsPhoneNumberUniqueAsync(countryCode, phoneNumber);
            if (!isUnique)
                return BadRequest(new { message = "Phone number is already registered to another account." });
        }

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
                CountryCode = countryCode ?? "+20",
                PhoneNumber = phoneNumber ?? "",
                SpecializationId = request.SpecializationId,
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
                        AvailabilityDays = request.ClinicAvailabilityDays ?? request.AvailabilityDays ?? "[\"Monday\",\"Tuesday\",\"Wednesday\",\"Thursday\",\"Friday\"]",
                        Latitude = request.Latitude,
                        Longitude = request.Longitude,
                        City = request.City,
                        State = request.State,
                        Country = request.Country
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
                    // No default clinic - doctor must be assigned to a clinic later
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
                CountryCode = countryCode ?? "+20",
                PhoneNumber = phoneNumber ?? "",
                Gender = request.Gender ?? "Male",
                DateOfBirth = request.Dob ?? "1996-01-01",
                BloodGroup = request.BloodGroup ?? "O+",
                Address = request.Address ?? "",
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                City = request.City,
                State = request.State,
                Country = request.Country,
                ClinicId = string.IsNullOrWhiteSpace(request.ClinicId) ? null : request.ClinicId,
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

        if (role == UserRole.Assistant)
        {
            var clinics = request.ClinicIds ?? new List<string>();
            if (!string.IsNullOrWhiteSpace(request.ClinicId) && !clinics.Contains(request.ClinicId))
            {
                clinics.Add(request.ClinicId);
            }
            newUser.UserClinics = clinics.Select(cid => new UserClinic { ClinicId = cid, UserId = newUser.Id }).ToList();
            registeredClinicIds = clinics;
        }

        await _userRepo.AddAsync(newUser);

        var clinicIds = registeredClinicIds ??
            (string.IsNullOrWhiteSpace(request.ClinicId) ? new List<string>() : new List<string> { request.ClinicId });
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
            var clinicIds = await GetUserClinicIds(u);
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
                profile.SpecializationId = doctor.SpecializationId;
                profile.Specialization = doctor.Specialization;
                profile.ContactNumber = doctor.ContactNumber;
                profile.CountryCode = doctor.CountryCode;
                profile.PhoneNumber = doctor.PhoneNumber;
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
                profile.CountryCode = patient.CountryCode;
                profile.PhoneNumber = patient.PhoneNumber;
                profile.Allergies = patient.Allergies;
                profile.ChronicDiseases = patient.ChronicDiseases;
                profile.PastIllnesses = patient.PastIllnesses;
                profile.Latitude = patient.Latitude;
                profile.Longitude = patient.Longitude;
                profile.City = patient.City;
                profile.State = patient.State;
                profile.Country = patient.Country;
            }
        }

        return Ok(new { data = profile });
    }

    [HttpPost("profile-send-otp")]
    [Authorize]
    public async Task<IActionResult> ProfileSendOtp([FromBody] ProfileOtpRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.ContactNumber))
            return BadRequest(new { message = "Email or Contact Number is required" });

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var existing = await _userRepo.GetByEmailAsync(request.Email);
            if (existing != null && existing.Id != userId)
                return BadRequest(new { message = "Email is already in use by another account" });

            var emailCode = _otpService.GenerateOtp(request.Email);
            await _emailService.SendEmailAsync(request.Email, "Clinic Profile Email Verification", $"Your profile email verification code is: <strong>{emailCode}</strong>. It is valid for 10 minutes.");
            return Ok(new { message = "OTP sent to your email successfully", emailOtp = emailCode });
        }

        var contactNumber = request.ContactNumber;
        var countryCode = request.CountryCode;
        var phoneNumber = request.PhoneNumber;

        if (string.IsNullOrEmpty(phoneNumber) && !string.IsNullOrEmpty(contactNumber))
        {
            var split = Clinic.Domain.Helpers.PhoneHelper.SplitContactNumber(contactNumber);
            countryCode = split.CountryCode;
            phoneNumber = split.PhoneNumber;
        }

        if (!string.IsNullOrEmpty(phoneNumber))
        {
            if (string.IsNullOrEmpty(countryCode))
            {
                countryCode = "+20";
            }

            var validation = Clinic.Domain.Helpers.PhoneHelper.ValidatePhoneNumber(countryCode, phoneNumber);
            if (!validation.IsValid)
                return BadRequest(new { message = validation.ErrorMessage });

            var normPhone = Clinic.Domain.Helpers.PhoneHelper.NormalizePhoneNumber(countryCode, phoneNumber);
            var isUnique = await _userRepo.IsPhoneNumberUniqueAsync(countryCode, normPhone, userId);
            if (!isUnique)
                return BadRequest(new { message = "Phone number is already in use by another account" });

            var fullContactNumber = $"{countryCode}{normPhone}";
            var (success, message, whatsappCode) = await _whatsappOtpService.RequestOtpAsync(fullContactNumber);
            if (!success)
            {
                if (message.Contains("Too many OTP requests", StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(429, new { message });
                }
                return BadRequest(new { message });
            }

            return Ok(new { message = "OTP sent to your phone via WhatsApp successfully", whatsappOtp = whatsappCode });
        }

        return BadRequest(new { message = "Invalid request" });
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

        // 1. Email OTP Check
        if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _userRepo.GetByEmailAsync(dto.Email);
            if (existing != null && existing.Id != userId)
                return BadRequest(new { message = "Email is already in use by another account" });

            if (string.IsNullOrWhiteSpace(dto.EmailOtpCode))
                return BadRequest(new { message = "Email verification code is required to update email" });

            if (!_otpService.VerifyOtp(dto.Email, dto.EmailOtpCode))
                return BadRequest(new { message = "Invalid or expired Email verification code" });

            _otpService.RemoveOtp(dto.Email);
        }

        Doctor? doctor = null;
        Patient? patient = null;

        if (user.Role == UserRole.Doctor && !string.IsNullOrEmpty(user.DoctorId))
        {
            doctor = await _doctorRepo.GetByIdAsync(user.DoctorId);
        }
        else if (user.Role == UserRole.Patient && !string.IsNullOrEmpty(user.PatientId))
        {
            patient = await _patientRepo.GetByIdAsync(user.PatientId);
        }

        // 2. Phone OTP Check
        var countryCode = dto.CountryCode;
        var phoneNumber = dto.PhoneNumber;

        if (string.IsNullOrEmpty(phoneNumber) && !string.IsNullOrEmpty(dto.ContactNumber))
        {
            var split = Clinic.Domain.Helpers.PhoneHelper.SplitContactNumber(dto.ContactNumber);
            countryCode = split.CountryCode;
            phoneNumber = split.PhoneNumber;
        }

        if (string.IsNullOrEmpty(countryCode))
        {
            countryCode = "+20";
        }

        var hasNewPhone = false;
        var normPhone = "";
        if (!string.IsNullOrEmpty(phoneNumber))
        {
            var validation = Clinic.Domain.Helpers.PhoneHelper.ValidatePhoneNumber(countryCode, phoneNumber);
            if (!validation.IsValid)
                return BadRequest(new { message = validation.ErrorMessage });

            normPhone = Clinic.Domain.Helpers.PhoneHelper.NormalizePhoneNumber(countryCode, phoneNumber);

            if (doctor != null && (!string.Equals(doctor.CountryCode, countryCode, StringComparison.OrdinalIgnoreCase) || !string.Equals(doctor.PhoneNumber, normPhone, StringComparison.OrdinalIgnoreCase)))
            {
                hasNewPhone = true;
            }
            else if (patient != null && (!string.Equals(patient.CountryCode, countryCode, StringComparison.OrdinalIgnoreCase) || !string.Equals(patient.PhoneNumber, normPhone, StringComparison.OrdinalIgnoreCase)))
            {
                hasNewPhone = true;
            }
        }

        if (hasNewPhone)
        {
            var isUnique = await _userRepo.IsPhoneNumberUniqueAsync(countryCode, normPhone, userId);
            if (!isUnique)
                return BadRequest(new { message = "Phone number is already in use by another account" });

            if (string.IsNullOrWhiteSpace(dto.PhoneOtpCode))
                return BadRequest(new { message = "WhatsApp verification code is required to update phone number" });

            var fullContactNumber = $"{countryCode}{normPhone}";
            if (!_whatsappOtpService.VerifyOtp(fullContactNumber, dto.PhoneOtpCode))
                return BadRequest(new { message = "Invalid or expired WhatsApp verification code" });

            _whatsappOtpService.RemoveOtp(fullContactNumber);
        }

        // Update core User
        user.Name = dto.Name;
        user.Email = dto.Email;

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        }

        await _userRepo.UpdateAsync(user);

        // Update Role-specific models
        if (doctor != null)
        {
            var nameParts = dto.Name.Split(' ', 2);
            doctor.FirstName = nameParts[0];
            doctor.LastName = nameParts.Length > 1 ? nameParts[1] : "";
            doctor.Email = dto.Email;
            doctor.SpecializationId = dto.SpecializationId ?? doctor.SpecializationId;
            doctor.Specialization = dto.Specialization ?? doctor.Specialization;
            if (!string.IsNullOrEmpty(phoneNumber))
            {
                doctor.CountryCode = countryCode;
                doctor.PhoneNumber = normPhone;
            }
            doctor.Avatar = dto.Avatar ?? doctor.Avatar;
            doctor.AvailabilityDays = dto.AvailabilityDays ?? doctor.AvailabilityDays;
            doctor.AvailabilityHours = dto.AvailabilityHours ?? doctor.AvailabilityHours;
            await _doctorRepo.UpdateAsync(doctor);
        }
        else if (patient != null)
        {
            var nameParts = dto.Name.Split(' ', 2);
            patient.FirstName = nameParts[0];
            patient.LastName = nameParts.Length > 1 ? nameParts[1] : "";
            patient.Email = dto.Email;
            patient.Gender = dto.Gender ?? patient.Gender;
            patient.DateOfBirth = dto.DateOfBirth ?? patient.DateOfBirth;
            patient.BloodGroup = dto.BloodGroup ?? patient.BloodGroup;
            patient.Address = dto.Address ?? patient.Address;
            patient.Latitude = dto.Latitude ?? patient.Latitude;
            patient.Longitude = dto.Longitude ?? patient.Longitude;
            patient.City = dto.City ?? patient.City;
            patient.State = dto.State ?? patient.State;
            patient.Country = dto.Country ?? patient.Country;
            if (!string.IsNullOrEmpty(phoneNumber))
            {
                patient.CountryCode = countryCode;
                patient.PhoneNumber = normPhone;
            }
            patient.Allergies = dto.Allergies ?? patient.Allergies;
            patient.ChronicDiseases = dto.ChronicDiseases ?? patient.ChronicDiseases;
            patient.PastIllnesses = dto.PastIllnesses ?? patient.PastIllnesses;
            await _patientRepo.UpdateAsync(patient);
        }

        return Ok(new { message = "Profile updated successfully", data = dto });
    }

    // ── Forgot / Reset Password ──
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "Email is required" });

        var user = await _userRepo.GetByEmailAsync(request.Email);
        if (user == null)
            return NotFound(new { message = "No account found with this email address" });

        var code = _otpService.GenerateOtp(request.Email);
        await _emailService.SendEmailAsync(request.Email, "Password Reset Code",
            $"Your password reset code is: <strong>{code}</strong>. It is valid for 10 minutes.");

        return Ok(new { message = "Password reset code sent to your email", otp = code });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Code) ||
            string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { message = "Email, code, and new password are required" });

        if (request.NewPassword.Length < 6)
            return BadRequest(new { message = "Password must be at least 6 characters long" });

        if (!_otpService.VerifyOtp(request.Email, request.Code))
            return BadRequest(new { message = "Invalid or expired reset code" });

        _otpService.RemoveOtp(request.Email);

        var user = await _userRepo.GetByEmailAsync(request.Email);
        if (user == null)
            return NotFound(new { message = "No account found with this email address" });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _userRepo.UpdateAsync(user);

        return Ok(new { message = "Password reset successfully. You can now log in with your new password." });
    }

    // ── Helpers ──
    private async Task<List<string>?> GetUserClinicIds(User user)
    {
        if (user.Role == UserRole.Doctor && !string.IsNullOrEmpty(user.DoctorId))
        {
            var doctor = await _doctorRepo.GetByIdAsync(user.DoctorId);
            if (doctor == null) return null;

            // Load doctor with clinics
            var doctors = await _doctorRepo.GetAllAsync(); // includes DoctorClinics
            var d = doctors.FirstOrDefault(x => x.Id == user.DoctorId);
            return d?.DoctorClinics.Select(dc => dc.ClinicId).ToList();
        }
        else if (user.Role == UserRole.Assistant)
        {
            var fullUser = await _userRepo.GetByIdAsync(user.Id);
            return fullUser?.UserClinics?.Select(uc => uc.ClinicId).ToList() ?? new List<string>();
        }
        return null;
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
