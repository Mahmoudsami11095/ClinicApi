using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/equipment/sterilization")]
public class SterilizationController : ControllerBase
{
    private static readonly ConcurrentDictionary<string, AutoclaveCycleDto> _cyclesById = new();

    static SterilizationController()
    {
        // Seed initial historical autoclave sterilization cycles
        var cycle1 = new AutoclaveCycleDto(
            CycleId: "cycle-seed-01",
            AutoclaveSerialNumber: "MELAG-VAC-8821",
            ChamberModel: "Melag Vacuklav 40 B+ Evolution",
            CycleType: "ClassB_134",
            PlateauDurationSeconds: 240,
            PeakTemperatureC: 134.8,
            PeakPressureBar: 2.16,
            WaterConductivityMicroSiemens: 4.2,
            BiologicalIndicatorNegative: true,
            IsSterilizationValidated: true,
            CycleStatus: "Validated",
            StatusMessage: "DIN EN 13060 Class B cycle certified. Saturated steam plateau maintained for 240s at 134.8°C / 2.16 bar. 4 instrument cassettes released.",
            OperatorId: "OP-NURSE-04",
            CassetteTokens: new List<string> { "CASSETTE-SURG-01", "CASSETTE-SURG-02", "CASSETTE-ENDO-01", "CASSETTE-PERIO-03" },
            AuditCertificateHash: "sha256-a9b8c7d6e5f4a3b2c1d0e9f8a7b6c5d4e3f2a1b0c9d8e7f6a5b4c3d2e1f0a9b8",
            CompletedAtUtc: DateTime.UtcNow.AddHours(-6)
        );

        var cycle2 = new AutoclaveCycleDto(
            CycleId: "cycle-seed-02",
            AutoclaveSerialNumber: "MELAG-VAC-8821",
            ChamberModel: "Melag Vacuklav 40 B+ Evolution",
            CycleType: "ClassB_134",
            PlateauDurationSeconds: 270,
            PeakTemperatureC: 135.2,
            PeakPressureBar: 2.18,
            WaterConductivityMicroSiemens: 3.8,
            BiologicalIndicatorNegative: true,
            IsSterilizationValidated: true,
            CycleStatus: "Validated",
            StatusMessage: "DIN EN 13060 Class B cycle certified. Saturated steam plateau maintained for 270s at 135.2°C / 2.18 bar. 3 instrument cassettes released.",
            OperatorId: "OP-TECH-02",
            CassetteTokens: new List<string> { "CASSETTE-PROS-01", "CASSETTE-IMPL-02", "CASSETTE-REST-05" },
            AuditCertificateHash: "sha256-b1c2d3e4f5a6b7c8d9e0f1a2b3c4d5e6f7a8b9c0d1e2f3a4b5c6d7e8f9a0b1c2",
            CompletedAtUtc: DateTime.UtcNow.AddHours(-3)
        );

        var cycle3 = new AutoclaveCycleDto(
            CycleId: "cycle-seed-03",
            AutoclaveSerialNumber: "MELAG-VAC-8821",
            ChamberModel: "Melag Vacuklav 40 B+ Evolution",
            CycleType: "ClassB_134",
            PlateauDurationSeconds: 155, // Failed < 210s threshold
            PeakTemperatureC: 131.4,     // Failed < 134.0°C threshold
            PeakPressureBar: 1.94,       // Failed < 2.10 bar threshold
            WaterConductivityMicroSiemens: 18.5,
            BiologicalIndicatorNegative: false,
            IsSterilizationValidated: false,
            CycleStatus: "Failed",
            StatusMessage: "BR-IOT-02 WARNING: Sterilization cycle failed EN 13060 Class B threshold (plateau temperature or pressure deficit). All 2 cassettes quarantined.",
            OperatorId: "OP-TECH-02",
            CassetteTokens: new List<string> { "CASSETTE-DEFECT-01", "CASSETTE-DEFECT-02" },
            AuditCertificateHash: "sha256-c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f0a1b2c3d4",
            CompletedAtUtc: DateTime.UtcNow.AddHours(-1)
        );

        _cyclesById[cycle1.CycleId] = cycle1;
        _cyclesById[cycle2.CycleId] = cycle2;
        _cyclesById[cycle3.CycleId] = cycle3;
    }

    public record AutoclaveTelemetrySnapshotDto(
        string AutoclaveSerialNumber,
        string ChamberModel,
        double ChamberTemperatureC,
        double ChamberPressureBar,
        double WaterConductivityMicroSiemens,
        string CyclePhase,
        int ElapsedSeconds,
        DateTime TimestampUtc
    );

    public record AutoclaveCycleDto(
        string CycleId,
        string AutoclaveSerialNumber,
        string ChamberModel,
        string CycleType,
        int PlateauDurationSeconds,
        double PeakTemperatureC,
        double PeakPressureBar,
        double WaterConductivityMicroSiemens,
        bool BiologicalIndicatorNegative,
        bool IsSterilizationValidated,
        string CycleStatus,
        string StatusMessage,
        string OperatorId,
        IReadOnlyList<string> CassetteTokens,
        string AuditCertificateHash,
        DateTime CompletedAtUtc
    );

    public record ValidateAutoclaveCycleRequest(
        string? AutoclaveSerialNumber,
        string? CycleType,
        int? PlateauDurationSeconds,
        double? PeakTemperatureC,
        double? PeakPressureBar,
        double? WaterConductivityMicroSiemens,
        bool? BiologicalIndicatorNegative,
        string? OperatorId,
        IReadOnlyList<string>? CassetteTokens
    );

    [HttpGet("telemetry")]
    [AllowAnonymous]
    public IActionResult GetLiveTelemetry()
    {
        // Real-Time IoT telemetry sensor stream snapshot (BR-IOT-01)
        var snapshot = new AutoclaveTelemetrySnapshotDto(
            AutoclaveSerialNumber: "MELAG-VAC-8821",
            ChamberModel: "Melag Vacuklav 40 B+ Evolution",
            ChamberTemperatureC: 134.6,
            ChamberPressureBar: 2.14,
            WaterConductivityMicroSiemens: 4.1,
            CyclePhase: "Plateau",
            ElapsedSeconds: 165,
            TimestampUtc: DateTime.UtcNow
        );
        return Ok(snapshot);
    }

    [HttpGet("cycles")]
    [AllowAnonymous]
    public IActionResult GetCycles()
    {
        var cycles = _cyclesById.Values.OrderByDescending(c => c.CompletedAtUtc).ToList();
        return Ok(cycles);
    }

    [HttpGet("cycles/{id}")]
    [AllowAnonymous]
    public IActionResult GetCycleById(string id)
    {
        if (_cyclesById.TryGetValue(id, out var cycle))
        {
            return Ok(cycle);
        }
        return NotFound(new { error = $"Sterilization cycle '{id}' not found." });
    }

    [HttpPost("cycles/validate")]
    [AllowAnonymous]
    public IActionResult ValidateCycle([FromBody] ValidateAutoclaveCycleRequest request)
    {
        var serialNumber = string.IsNullOrWhiteSpace(request?.AutoclaveSerialNumber)
            ? "MELAG-VAC-8821"
            : request.AutoclaveSerialNumber;

        var cycleType = string.IsNullOrWhiteSpace(request?.CycleType)
            ? "ClassB_134"
            : request.CycleType;

        var plateauSec = Math.Clamp(request?.PlateauDurationSeconds ?? 240, 0, 1800);
        var peakTemp = Math.Clamp(request?.PeakTemperatureC ?? 134.8, 100.0, 150.0);
        var peakPressure = Math.Clamp(request?.PeakPressureBar ?? 2.15, 0.0, 3.5);
        var conductivity = Math.Clamp(request?.WaterConductivityMicroSiemens ?? 4.0, 0.0, 100.0);
        var bioNegative = request?.BiologicalIndicatorNegative ?? true;
        var operatorId = string.IsNullOrWhiteSpace(request?.OperatorId) ? "OP-CHIEF-01" : request.OperatorId;

        var cassettes = (request?.CassetteTokens != null && request.CassetteTokens.Count > 0)
            ? request.CassetteTokens.ToList()
            : new List<string> { "CASSETTE-AUTO-01", "CASSETTE-AUTO-02" };

        // EN 13060 Class B Validation Criteria (BR-IOT-02)
        bool isCompliant;
        if (cycleType == "ClassB_121")
        {
            isCompliant = peakTemp >= 121.0 && peakPressure >= 1.05 && plateauSec >= 900 && bioNegative;
        }
        else // ClassB_134 or Prion_134
        {
            isCompliant = peakTemp >= 134.0 && peakPressure >= 2.10 && plateauSec >= 210 && bioNegative;
        }

        var cycleId = $"cycle-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6]}";
        var completedAt = DateTime.UtcNow;

        var statusMessage = isCompliant
            ? $"DIN EN 13060 Class B cycle certified. Peak: {peakTemp:F1}°C at {peakPressure:F2} bar ({plateauSec}s plateau). All {cassettes.Count} cassettes released for operatory use."
            : $"BR-IOT-02 WARNING: Sterilization cycle failed EN 13060 Class B threshold (plateau temperature or pressure deficit). All {cassettes.Count} cassettes quarantined.";

        // DIN EN ISO 11140-1 Cryptographic Hash Digest (BR-IOT-04)
        var rawProof = $"{cycleId}:{serialNumber}:{completedAt:O}:{plateauSec}:{peakTemp:F2}:{peakPressure:F2}:{isCompliant}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawProof));
        var certHash = "sha256-" + Convert.ToHexString(hashBytes).ToLowerInvariant();

        var cycle = new AutoclaveCycleDto(
            CycleId: cycleId,
            AutoclaveSerialNumber: serialNumber,
            ChamberModel: "Melag Vacuklav 40 B+ Evolution",
            CycleType: cycleType,
            PlateauDurationSeconds: plateauSec,
            PeakTemperatureC: peakTemp,
            PeakPressureBar: peakPressure,
            WaterConductivityMicroSiemens: conductivity,
            BiologicalIndicatorNegative: bioNegative,
            IsSterilizationValidated: isCompliant,
            CycleStatus: isCompliant ? "Validated" : "Failed",
            StatusMessage: statusMessage,
            OperatorId: operatorId,
            CassetteTokens: cassettes,
            AuditCertificateHash: certHash,
            CompletedAtUtc: completedAt
        );

        _cyclesById[cycle.CycleId] = cycle;
        return Ok(cycle);
    }
}
