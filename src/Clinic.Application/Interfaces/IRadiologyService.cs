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

    // PACS DICOM Modality & Multi-Slice CBCT (Release v4.1.0)
    Task<DicomMetadataDto> GetDicomMetadataAsync(string recordId);
    Task<DicomSeriesDto> GetDicomSlicesAsync(string recordId, string orientation = "Axial");

    // AI Cephalometric Landmark Tracing & Orthodontic Analysis (Release v8.2.0 & v11.6.0)
    Task<CephalometricAnalysisResultDto> GetCephalometricAnalysisAsync(string recordId);
    Task<CephalometricReportDto> SaveCephalometricReportAsync(string recordId, SaveCephalometricReportDto dto, string doctorId, string doctorName);
    Task<CephalometricReportDto?> GetCephalometricReportAsync(string recordId);
    Task<CephalometricAnalysisResultDto> RecalculateCephalometricAnalysisAsync(string recordId, RecalculateCephRequestDto request);
    Task<CephalometricVtoResultDto> SimulateSurgicalVtoAsync(string recordId, CephalometricVtoSimulationRequestDto request);

    // AI Endodontic Working Length & Root Canal Curvature Locator (Release v11.8.0)
    Task<EndodonticAnalysisResultDto> AnalyzeEndodonticCanalAsync(string recordId, int toothNumber = 36, string? canalName = null);
    Task<EndodonticAnalysisResultDto> RecalculateEndodonticCanalAsync(string recordId, RecalculateEndodonticCanalRequestDto request);
    Task<EndodonticAnalysisResultDto> SaveEndodonticReportAsync(string recordId, SaveEndodonticAnalysisRequestDto request, string doctorId, string doctorName);
}
