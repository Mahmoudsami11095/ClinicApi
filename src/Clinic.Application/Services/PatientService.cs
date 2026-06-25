using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Clinic.Application.Services;

public class PatientService : IPatientService
{
    private readonly IPatientRepository _repo;
    private readonly IClinicRepository _clinicRepo;
    private readonly IUserRepository _userRepo;

    public PatientService(IPatientRepository repo, IClinicRepository clinicRepo, IUserRepository userRepo)
    {
        _repo = repo;
        _clinicRepo = clinicRepo;
        _userRepo = userRepo;
    }

    public async Task<IEnumerable<PatientDto>> GetAllAsync(string? doctorIdClaim, string? clinicIdClaim = null)
    {
        var patients = await _repo.GetAllAsync();
        
        if (!string.IsNullOrEmpty(doctorIdClaim))
        {
            var clinics = await _clinicRepo.GetAllAsync();
            var allowedClinicIds = clinics
                .Where(c => c.CreatorDoctorId == doctorIdClaim || 
                            c.DoctorClinics.Any(dc => dc.DoctorId == doctorIdClaim && dc.Status == "Accepted"))
                .Select(c => c.Id)
                .ToList();
            patients = patients.Where(p => allowedClinicIds.Contains(p.ClinicId ?? "")).ToList();
        }
        else if (!string.IsNullOrEmpty(clinicIdClaim))
        {
            patients = patients.Where(p => p.ClinicId == clinicIdClaim).ToList();
        }

        return patients.Select(p => new PatientDto
        {
            Id = p.Id, FirstName = p.FirstName, LastName = p.LastName,
            Gender = p.Gender, DateOfBirth = p.DateOfBirth,
            ContactNumber = p.ContactNumber,
            CountryCode = p.CountryCode,
            PhoneNumber = p.PhoneNumber,
            Email = p.Email,
            BloodGroup = p.BloodGroup, Address = p.Address,
            Latitude = p.Latitude, Longitude = p.Longitude,
            City = p.City, State = p.State, Country = p.Country,
            RegistrationDate = p.RegistrationDate, ClinicId = p.ClinicId,
            Allergies = p.Allergies, ChronicDiseases = p.ChronicDiseases, PastIllnesses = p.PastIllnesses
        }).ToList();
    }

    public async Task CreateAsync(PatientDto dto, string? doctorIdClaim, string? clinicIdClaim = null)
    {
        if (!string.IsNullOrEmpty(doctorIdClaim))
        {
            var clinics = await _clinicRepo.GetAllAsync();
            var isAllowed = clinics.Any(c => c.Id == dto.ClinicId && 
                (c.CreatorDoctorId == doctorIdClaim || 
                 c.DoctorClinics.Any(dc => dc.DoctorId == doctorIdClaim && dc.Status == "Accepted")));
            if (!isAllowed)
                throw new UnauthorizedAccessException("You can only manage patients for your clinics");
        }
        else if (!string.IsNullOrEmpty(clinicIdClaim))
        {
            if (dto.ClinicId != clinicIdClaim)
                throw new UnauthorizedAccessException("You can only manage patients for your assigned clinic");
        }

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

        var validation = Clinic.Domain.Helpers.PhoneHelper.ValidatePhoneNumber(countryCode, phoneNumber);
        if (!validation.IsValid)
            throw new ArgumentException(validation.ErrorMessage);

        var normPhone = Clinic.Domain.Helpers.PhoneHelper.NormalizePhoneNumber(countryCode, phoneNumber!);

        var isUnique = await _userRepo.IsPhoneNumberUniqueAsync(countryCode, normPhone);
        if (!isUnique)
            throw new InvalidOperationException("This phone number is already registered to another account.");

        var entity = new Patient
        {
            Id = string.IsNullOrEmpty(dto.Id) ? Guid.NewGuid().ToString() : dto.Id,
            FirstName = dto.FirstName, LastName = dto.LastName,
            Gender = dto.Gender, DateOfBirth = dto.DateOfBirth,
            CountryCode = countryCode,
            PhoneNumber = normPhone,
            Email = dto.Email,
            BloodGroup = dto.BloodGroup, Address = dto.Address,
            Latitude = dto.Latitude, Longitude = dto.Longitude,
            City = dto.City, State = dto.State, Country = dto.Country,
            RegistrationDate = DateTime.UtcNow.ToString("yyyy-MM-dd"), ClinicId = dto.ClinicId,
            Allergies = dto.Allergies, ChronicDiseases = dto.ChronicDiseases, PastIllnesses = dto.PastIllnesses
        };
        await _repo.AddAsync(entity);
    }

    public async Task UpdateAsync(string id, PatientDto dto, string? doctorIdClaim, string? clinicIdClaim = null)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null)
            throw new KeyNotFoundException("Patient not found");

        if (!string.IsNullOrEmpty(doctorIdClaim))
        {
            var clinics = await _clinicRepo.GetAllAsync();
            var isAllowed = clinics.Any(c => c.Id == dto.ClinicId && 
                (c.CreatorDoctorId == doctorIdClaim || 
                 c.DoctorClinics.Any(dc => dc.DoctorId == doctorIdClaim && dc.Status == "Accepted")));
            if (!isAllowed)
                throw new UnauthorizedAccessException("You can only manage patients for your clinics");
        }
        else if (!string.IsNullOrEmpty(clinicIdClaim))
        {
            if (dto.ClinicId != clinicIdClaim || existing.ClinicId != clinicIdClaim)
                throw new UnauthorizedAccessException("You can only manage patients for your assigned clinic");
        }

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

        var validation = Clinic.Domain.Helpers.PhoneHelper.ValidatePhoneNumber(countryCode, phoneNumber);
        if (!validation.IsValid)
            throw new ArgumentException(validation.ErrorMessage);

        var normPhone = Clinic.Domain.Helpers.PhoneHelper.NormalizePhoneNumber(countryCode, phoneNumber!);

        var allUsers = await _userRepo.GetAllAsync();
        var linkedUser = allUsers.FirstOrDefault(u => u.PatientId == id);

        var isUnique = await _userRepo.IsPhoneNumberUniqueAsync(countryCode, normPhone, linkedUser?.Id);
        if (!isUnique)
            throw new InvalidOperationException("This phone number is already registered to another account.");

        existing.FirstName = dto.FirstName;
        existing.LastName = dto.LastName;
        existing.Gender = dto.Gender;
        existing.DateOfBirth = dto.DateOfBirth;
        existing.CountryCode = countryCode;
        existing.PhoneNumber = normPhone;
        existing.Email = dto.Email;
        existing.BloodGroup = dto.BloodGroup;
        existing.Address = dto.Address;
        existing.Latitude = dto.Latitude;
        existing.Longitude = dto.Longitude;
        existing.City = dto.City;
        existing.State = dto.State;
        existing.Country = dto.Country;
        existing.ClinicId = dto.ClinicId;
        existing.Allergies = dto.Allergies;
        existing.ChronicDiseases = dto.ChronicDiseases;
        existing.PastIllnesses = dto.PastIllnesses;

        await _repo.UpdateAsync(existing);
    }

    public async Task DeleteAsync(string id, string? doctorIdClaim, string? clinicIdClaim = null)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null)
            throw new KeyNotFoundException("Patient not found");

        if (!string.IsNullOrEmpty(doctorIdClaim))
        {
            var clinics = await _clinicRepo.GetAllAsync();
            var isAllowed = clinics.Any(c => c.Id == existing.ClinicId && 
                (c.CreatorDoctorId == doctorIdClaim || 
                 c.DoctorClinics.Any(dc => dc.DoctorId == doctorIdClaim && dc.Status == "Accepted")));
            if (!isAllowed)
                throw new UnauthorizedAccessException("You can only manage patients for your clinics");
        }
        else if (!string.IsNullOrEmpty(clinicIdClaim))
        {
            if (existing.ClinicId != clinicIdClaim)
                throw new UnauthorizedAccessException("You can only manage patients for your assigned clinic");
        }

        await _repo.DeleteAsync(id);
    }
}
