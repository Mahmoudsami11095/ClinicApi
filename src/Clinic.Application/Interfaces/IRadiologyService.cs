using Clinic.Application.DTOs;

namespace Clinic.Application.Interfaces;

public interface IRadiologyService
{
    // Radiology Centers
    Task<List<RadiologyCenterDto>> GetCentersAsync();
    Task<RadiologyCenterDto> CreateCenterAsync(CreateRadiologyCenterDto dto);
    Task<RadiologyCenterDto> UpdateCenterAsync(string id, CreateRadiologyCenterDto dto);
    Task DeleteCenterAsync(string id);

    // Radiology Records
    Task<List<RadiologyRecordDto>> GetRecordsAsync();
    Task<List<RadiologyRecordDto>> GetRecordsByDoctorAsync(string doctorId);
    Task<RadiologyRecordDto> CreateRecordAsync(CreateRadiologyRecordDto dto);
    Task<RadiologyRecordDto> UpdateRecordAsync(string id, CreateRadiologyRecordDto dto);
    Task DeleteRecordAsync(string id);

    // AI Computer Vision Diagnostics (Release v4.0.0)
    Task<AiRadiologyAnalysisResultDto> AnalyzeScanAsync(string recordId);
    Task<bool> SyncFindingsToOdontogramAsync(string recordId, SyncAiFindingsRequestDto request);
}
