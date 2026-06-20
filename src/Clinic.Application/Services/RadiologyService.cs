using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;

namespace Clinic.Application.Services;

public class RadiologyService : IRadiologyService
{
    private readonly IRadiologyCenterRepository _centerRepo;
    private readonly IRadiologyRecordRepository _recordRepo;
    private readonly IUserRepository _userRepo;

    public RadiologyService(
        IRadiologyCenterRepository centerRepo,
        IRadiologyRecordRepository recordRepo,
        IUserRepository userRepo)
    {
        _centerRepo = centerRepo;
        _recordRepo = recordRepo;
        _userRepo = userRepo;
    }

    public async Task<List<RadiologyCenterDto>> GetCentersAsync()
    {
        var centers = await _centerRepo.GetAllAsync();
        return centers.Select(c => new RadiologyCenterDto
        {
            Id = c.Id,
            Name = c.Name,
            ContactNumber = c.ContactNumber,
            Address = c.Address
        }).ToList();
    }

    public async Task<RadiologyCenterDto> CreateCenterAsync(CreateRadiologyCenterDto dto)
    {
        var center = new RadiologyCenter
        {
            Id = Guid.NewGuid().ToString(),
            Name = dto.Name,
            ContactNumber = dto.ContactNumber,
            Address = dto.Address
        };

        await _centerRepo.AddAsync(center);
        return new RadiologyCenterDto
        {
            Id = center.Id,
            Name = center.Name,
            ContactNumber = center.ContactNumber,
            Address = center.Address
        };
    }

    public async Task<RadiologyCenterDto> UpdateCenterAsync(string id, CreateRadiologyCenterDto dto)
    {
        var center = await _centerRepo.GetByIdAsync(id);
        if (center == null) throw new Exception("Radiology center not found.");

        center.Name = dto.Name;
        center.ContactNumber = dto.ContactNumber;
        center.Address = dto.Address;

        await _centerRepo.UpdateAsync(center);
        return new RadiologyCenterDto
        {
            Id = center.Id,
            Name = center.Name,
            ContactNumber = center.ContactNumber,
            Address = center.Address
        };
    }

    public async Task DeleteCenterAsync(string id)
    {
        await _centerRepo.DeleteAsync(id);
    }

    public async Task<List<RadiologyRecordDto>> GetRecordsAsync()
    {
        var records = await _recordRepo.GetAllAsync();
        // Since GenericRepository might not include navigation properties, we'll fetch basic list.
        // We really should use Include but GenericRepository doesn't support Include by default.
        // For now, we will just return basic or manually populate.
        
        var users = await _userRepo.GetAllAsync();
        var centers = await _centerRepo.GetAllAsync();

        return records.Select(r => new RadiologyRecordDto
        {
            Id = r.Id,
            DoctorId = r.DoctorId,
            PatientId = r.PatientId,
            RadiologyCenterId = r.RadiologyCenterId,
            ProcedureName = r.ProcedureName,
            AmountPaid = r.AmountPaid,
            Date = r.Date,
            Notes = r.Notes,
            // Basic manual lookup for names
            PatientName = users.FirstOrDefault(u => u.PatientId == r.PatientId)?.Name ?? "Unknown",
            RadiologyCenterName = centers.FirstOrDefault(c => c.Id == r.RadiologyCenterId)?.Name ?? "Unknown Center"
        }).ToList();
    }

    public async Task<List<RadiologyRecordDto>> GetRecordsByDoctorAsync(string doctorId)
    {
        var records = await GetRecordsAsync();
        return records.Where(r => r.DoctorId == doctorId).ToList();
    }

    public async Task<RadiologyRecordDto> CreateRecordAsync(CreateRadiologyRecordDto dto)
    {
        var record = new RadiologyRecord
        {
            Id = Guid.NewGuid().ToString(),
            DoctorId = dto.DoctorId,
            PatientId = dto.PatientId,
            RadiologyCenterId = dto.RadiologyCenterId,
            ProcedureName = dto.ProcedureName,
            AmountPaid = dto.AmountPaid,
            Date = dto.Date,
            Notes = dto.Notes
        };

        await _recordRepo.AddAsync(record);
        
        return new RadiologyRecordDto
        {
            Id = record.Id,
            DoctorId = record.DoctorId,
            PatientId = record.PatientId,
            RadiologyCenterId = record.RadiologyCenterId,
            ProcedureName = record.ProcedureName,
            AmountPaid = record.AmountPaid,
            Date = record.Date,
            Notes = record.Notes
        };
    }

    public async Task<RadiologyRecordDto> UpdateRecordAsync(string id, CreateRadiologyRecordDto dto)
    {
        var record = await _recordRepo.GetByIdAsync(id);
        if (record == null) throw new Exception("Radiology record not found.");

        record.DoctorId = dto.DoctorId;
        record.PatientId = dto.PatientId;
        record.RadiologyCenterId = dto.RadiologyCenterId;
        record.ProcedureName = dto.ProcedureName;
        record.AmountPaid = dto.AmountPaid;
        record.Date = dto.Date;
        record.Notes = dto.Notes;

        await _recordRepo.UpdateAsync(record);
        return new RadiologyRecordDto
        {
            Id = record.Id,
            DoctorId = record.DoctorId,
            PatientId = record.PatientId,
            RadiologyCenterId = record.RadiologyCenterId,
            ProcedureName = record.ProcedureName,
            AmountPaid = record.AmountPaid,
            Date = record.Date,
            Notes = record.Notes
        };
    }

    public async Task DeleteRecordAsync(string id)
    {
        await _recordRepo.DeleteAsync(id);
    }
}
