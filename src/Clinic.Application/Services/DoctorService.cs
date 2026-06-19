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

    public DoctorService(IDoctorRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<DoctorDto>> GetAllAsync()
    {
        var doctors = await _repo.GetAllAsync();
        return doctors.Select(MapToDto).ToList();
    }

    public async Task CreateAsync(DoctorDto dto)
    {
        var clinicIds = dto.ClinicIds ?? new List<string>();
        var entity = new Doctor
        {
            Id = string.IsNullOrEmpty(dto.Id) ? Guid.NewGuid().ToString() : dto.Id,
            FirstName = dto.FirstName, LastName = dto.LastName,
            Specialization = dto.Specialization, Email = dto.Email,
            ContactNumber = dto.ContactNumber, Avatar = dto.Avatar,
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
            ContactNumber = d.ContactNumber, Avatar = d.Avatar,
            Availability = new DoctorAvailabilityDto { Days = days, Hours = d.AvailabilityHours },
            ClinicIds = d.DoctorClinics?.Select(dc => dc.ClinicId).ToList(),
            ClinicAvailabilities = clinicAvails
        };
    }
}
