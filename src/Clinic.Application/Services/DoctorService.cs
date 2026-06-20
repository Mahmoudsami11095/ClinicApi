using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Clinic.Application.Services;

public class DoctorService : IDoctorService
{
    private readonly IDoctorRepository _repo;
    private readonly IUserRepository _userRepo;

    public DoctorService(IDoctorRepository repo, IUserRepository userRepo)
    {
        _repo = repo;
        _userRepo = userRepo;
    }

    public async Task<IEnumerable<DoctorDto>> GetAllAsync()
    {
        var doctors = await _repo.GetAllAsync();
        return doctors.Select(MapToDto).ToList();
    }

    public async Task CreateAsync(DoctorDto dto)
    {
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

        var clinicIds = dto.ClinicIds ?? new List<string>();
        var entity = new Doctor
        {
            Id = string.IsNullOrEmpty(dto.Id) ? Guid.NewGuid().ToString() : dto.Id,
            FirstName = dto.FirstName, LastName = dto.LastName,
            Specialization = dto.Specialization, Email = dto.Email,
            CountryCode = countryCode,
            PhoneNumber = normPhone,
            Avatar = dto.Avatar,
            AvailabilityDays = JsonSerializer.Serialize(dto.Availability?.Days ?? new List<string>()),
            AvailabilityHours = dto.Availability?.Hours ?? ""
        };
        await _repo.AddWithClinicsAsync(entity, clinicIds);
    }

    private static DoctorDto MapToDto(Doctor d)
    {
        List<string> days;
        try { days = JsonSerializer.Deserialize<List<string>>(d.AvailabilityDays) ?? new(); }
        catch { days = new List<string>(); }

        var clinicAvails = d.DoctorClinics?.Select(dc => {
            List<string> cDays;
            try { cDays = !string.IsNullOrEmpty(dc.AvailabilityDays) ? JsonSerializer.Deserialize<List<string>>(dc.AvailabilityDays) ?? new() : new(); }
            catch { cDays = new List<string>(); }

            return new DoctorClinicAvailabilityDto
            {
                ClinicId = dc.ClinicId,
                AvailabilityHours = dc.AvailabilityHours ?? "",
                AvailabilityDays = cDays
            };
        }).ToList();

        return new DoctorDto
        {
            Id = d.Id, FirstName = d.FirstName, LastName = d.LastName,
            Specialization = d.Specialization, Email = d.Email,
            ContactNumber = d.ContactNumber,
            CountryCode = d.CountryCode,
            PhoneNumber = d.PhoneNumber,
            Avatar = d.Avatar,
            Availability = new DoctorAvailabilityDto { Days = days, Hours = d.AvailabilityHours },
            ClinicIds = d.DoctorClinics?.Select(dc => dc.ClinicId).ToList(),
            ClinicAvailabilities = clinicAvails
        };
    }
}
