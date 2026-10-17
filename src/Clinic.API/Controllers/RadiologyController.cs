using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RadiologyController : ControllerBase
{
    private readonly IRadiologyService _radiologyService;

    public RadiologyController(IRadiologyService radiologyService)
    {
        _radiologyService = radiologyService;
    }

    // ── Centers ──
    [HttpGet("centers")]
    public async Task<IActionResult> GetCenters()
    {
        var centers = await _radiologyService.GetCentersAsync();
        return Ok(centers);
    }

    [HttpPost("centers")]
    [Authorize(Roles = "admin,doctor,assistant")]
    public async Task<IActionResult> CreateCenter([FromBody] CreateRadiologyCenterDto dto)
    {
        var center = await _radiologyService.CreateCenterAsync(dto);
        return CreatedAtAction(nameof(GetCenters), new { id = center.Id }, center);
    }

    [HttpPut("centers/{id}")]
    [Authorize(Roles = "admin,doctor,assistant")]
    public async Task<IActionResult> UpdateCenter(string id, [FromBody] CreateRadiologyCenterDto dto)
    {
        var center = await _radiologyService.UpdateCenterAsync(id, dto);
        return Ok(center);
    }

    [HttpDelete("centers/{id}")]
    [Authorize(Roles = "admin,doctor")]
    public async Task<IActionResult> DeleteCenter(string id)
    {
        await _radiologyService.DeleteCenterAsync(id);
        return NoContent();
    }

    // ── Records ──
    [HttpGet("records")]
    public async Task<IActionResult> GetRecords([FromQuery] string? doctorId)
    {
        if (!string.IsNullOrEmpty(doctorId))
        {
            var doctorRecords = await _radiologyService.GetRecordsByDoctorAsync(doctorId);
            return Ok(doctorRecords);
        }
        var records = await _radiologyService.GetRecordsAsync();
        return Ok(records);
    }

    [HttpPost("records")]
    [Authorize(Roles = "admin,doctor,assistant")]
    public async Task<IActionResult> CreateRecord([FromBody] CreateRadiologyRecordDto dto)
    {
        var record = await _radiologyService.CreateRecordAsync(dto);
        return CreatedAtAction(nameof(GetRecords), new { id = record.Id }, record);
    }

    [HttpPut("records/{id}")]
    [Authorize(Roles = "admin,doctor,assistant")]
    public async Task<IActionResult> UpdateRecord(string id, [FromBody] CreateRadiologyRecordDto dto)
    {
        var record = await _radiologyService.UpdateRecordAsync(id, dto);
        return Ok(record);
    }

    [HttpDelete("records/{id}")]
    [Authorize(Roles = "admin,doctor,assistant")]
    public async Task<IActionResult> DeleteRecord(string id)
    {
        await _radiologyService.DeleteRecordAsync(id);
        return NoContent();
    }

    // ── AI Computer Vision Diagnostics (Release v4.0.0) ──
    [HttpPost("records/{id}/ai-analyze")]
    [Authorize(Roles = "admin,doctor,assistant")]
    public async Task<IActionResult> AnalyzeScanWithAi(string id)
    {
        var result = await _radiologyService.AnalyzeScanAsync(id);
        return Ok(result);
    }

    [HttpPost("records/{id}/ai-sync-odontogram")]
    [Authorize(Roles = "admin,doctor,assistant")]
    public async Task<IActionResult> SyncAiFindingsToOdontogram(string id, [FromBody] SyncAiFindingsRequestDto request)
    {
        var success = await _radiologyService.SyncFindingsToOdontogramAsync(id, request);
        return Ok(new { success, message = "AI radiographic diagnostic findings successfully synchronized to patient dental chart." });
    }

    // ── PACS DICOM Web Modality & HU Presets (Release v4.1.0) ──
    [HttpGet("records/{id}/dicom-metadata")]
    public async Task<IActionResult> GetDicomMetadata(string id)
    {
        var metadata = await _radiologyService.GetDicomMetadataAsync(id);
        return Ok(metadata);
    }

    [HttpGet("records/{id}/dicom-slices")]
    public async Task<IActionResult> GetDicomSlices(string id, [FromQuery] string orientation = "Axial")
    {
        var series = await _radiologyService.GetDicomSlicesAsync(id, orientation);
        return Ok(series);
    }

    // ── AI Cephalometric Landmark Tracing & Orthodontic Analysis (Release v8.2.0) ──
    [HttpGet("records/{id}/ceph-analysis")]
    [Authorize(Roles = "admin,doctor,assistant")]
    public async Task<IActionResult> GetCephalometricAnalysis(string id)
    {
        var result = await _radiologyService.GetCephalometricAnalysisAsync(id);
        return Ok(result);
    }

    [HttpPost("records/{id}/ceph-report")]
    [Authorize(Roles = "admin,doctor,assistant")]
    public async Task<IActionResult> SaveCephalometricReport(string id, [FromBody] SaveCephalometricReportDto dto)
    {
        var doctorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "doc-rad-1";
        var doctorName = User.Identity?.Name ?? "Attending Orthodontist";
        var report = await _radiologyService.SaveCephalometricReportAsync(id, dto, doctorId, doctorName);
        return Ok(report);
    }

    [HttpGet("records/{id}/ceph-report")]
    [Authorize(Roles = "admin,doctor,assistant")]
    public async Task<IActionResult> GetCephalometricReport(string id)
    {
        var report = await _radiologyService.GetCephalometricReportAsync(id);
        if (report == null)
            return NotFound(new { message = "No cephalometric report found for this record." });
        return Ok(report);
    }

    [HttpPost("records/{id}/ceph-recalculate")]
    [Authorize(Roles = "admin,doctor,assistant")]
    public async Task<IActionResult> RecalculateCephalometricAnalysis(string id, [FromBody] RecalculateCephRequestDto request)
    {
        var result = await _radiologyService.RecalculateCephalometricAnalysisAsync(id, request);
        return Ok(result);
    }

    [HttpPost("records/{id}/ceph-vto")]
    [Authorize(Roles = "admin,doctor,assistant")]
    public async Task<IActionResult> SimulateSurgicalVto(string id, [FromBody] CephalometricVtoSimulationRequestDto request)
    {
        var result = await _radiologyService.SimulateSurgicalVtoAsync(id, request);
        return Ok(result);
    }

    // ── AI Endodontic Working Length & Root Canal Curvature Locator (Release v11.8.0) ──
    [HttpGet("records/{id}/endodontic")]
    [Authorize(Roles = "admin,doctor,assistant")]
    public async Task<IActionResult> GetEndodonticAnalysis(string id, [FromQuery] int toothNumber = 36, [FromQuery] string? canalName = null)
    {
        var result = await _radiologyService.AnalyzeEndodonticCanalAsync(id, toothNumber, canalName);
        return Ok(result);
    }

    [HttpPost("records/{id}/endodontic/recalculate")]
    [Authorize(Roles = "admin,doctor,assistant")]
    public async Task<IActionResult> RecalculateEndodonticCanal(string id, [FromBody] RecalculateEndodonticCanalRequestDto request)
    {
        var result = await _radiologyService.RecalculateEndodonticCanalAsync(id, request);
        return Ok(result);
    }

    [HttpPost("records/{id}/endodontic")]
    [Authorize(Roles = "admin,doctor,assistant")]
    public async Task<IActionResult> SaveEndodonticReport(string id, [FromBody] SaveEndodonticAnalysisRequestDto request)
    {
        var doctorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "doc-endo-1";
        var doctorName = User.Identity?.Name ?? "Attending Endodontist";
        var result = await _radiologyService.SaveEndodonticReportAsync(id, request, doctorId, doctorName);
        return Ok(result);
    }
}
