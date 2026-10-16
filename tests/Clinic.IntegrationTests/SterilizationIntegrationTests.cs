using System.Net.Http.Json;
using System.Text.Json;
using Clinic.API.Controllers;
using Xunit;

namespace Clinic.IntegrationTests;

public class SterilizationIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public SterilizationIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task BR_IOT_01_GetLiveTelemetry_Returns_Nominal_Sensors()
    {
        // Act
        var response = await _client.GetAsync("/api/sterilization/telemetry");

        // Assert
        response.EnsureSuccessStatusCode();
        var telemetry = await response.Content.ReadFromJsonAsync<SterilizationController.AutoclaveTelemetrySnapshotDto>(_jsonOptions);

        Assert.NotNull(telemetry);
        Assert.Equal("MELAG-VAC-8821", telemetry.AutoclaveSerialNumber);
        Assert.Equal(134.6, telemetry.ChamberTemperatureC);
        Assert.Equal(2.14, telemetry.ChamberPressureBar);
        Assert.Equal(4.1, telemetry.WaterConductivityMicroSiemens);
        Assert.Equal("Plateau", telemetry.CyclePhase);
    }

    [Fact]
    public async Task BR_IOT_02_ValidateClassBCycle_Passes_When_Criteria_Met()
    {
        // Arrange: Meets EN 13060 Class B criteria (135.0°C, 2.15 bar, 240s plateau, bioNegative = true)
        var req = new SterilizationController.ValidateAutoclaveCycleRequest(
            AutoclaveSerialNumber: "MELAG-VAC-8821",
            CycleType: "ClassB_134",
            PlateauDurationSeconds: 240,
            PeakTemperatureC: 135.0,
            PeakPressureBar: 2.15,
            WaterConductivityMicroSiemens: 3.5,
            BiologicalIndicatorNegative: true,
            OperatorId: "OP-DOC-01",
            CassetteTokens: new List<string> { "CASSETTE-SURG-TEST-01", "CASSETTE-SURG-TEST-02" }
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/sterilization/cycles/validate", req);

        // Assert
        response.EnsureSuccessStatusCode();
        var cycle = await response.Content.ReadFromJsonAsync<SterilizationController.AutoclaveCycleDto>(_jsonOptions);

        Assert.NotNull(cycle);
        Assert.True(cycle.IsSterilizationValidated);
        Assert.Equal("Validated", cycle.CycleStatus);
        Assert.Contains("DIN EN 13060 Class B cycle certified", cycle.StatusMessage);
        Assert.Contains("released", cycle.StatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("sha256-", cycle.AuditCertificateHash);
        Assert.Equal(2, cycle.CassetteTokens.Count);
    }

    [Fact]
    public async Task BR_IOT_02_ValidateClassBCycle_Fails_And_Quarantines_On_Temperature_Drop()
    {
        // Arrange: Sub-threshold peak temperature 130.5°C (< 134.0°C)
        var req = new SterilizationController.ValidateAutoclaveCycleRequest(
            AutoclaveSerialNumber: "MELAG-VAC-8821",
            CycleType: "ClassB_134",
            PlateauDurationSeconds: 240,
            PeakTemperatureC: 130.5, // Failed threshold
            PeakPressureBar: 2.15,
            WaterConductivityMicroSiemens: 4.0,
            BiologicalIndicatorNegative: true,
            OperatorId: "OP-DOC-01",
            CassetteTokens: new List<string> { "CASSETTE-QUARANTINE-01" }
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/sterilization/cycles/validate", req);

        // Assert
        response.EnsureSuccessStatusCode();
        var cycle = await response.Content.ReadFromJsonAsync<SterilizationController.AutoclaveCycleDto>(_jsonOptions);

        Assert.NotNull(cycle);
        Assert.False(cycle.IsSterilizationValidated);
        Assert.Equal("Failed", cycle.CycleStatus);
        Assert.Contains("BR-IOT-02 WARNING", cycle.StatusMessage);
        Assert.Contains("quarantined", cycle.StatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("sha256-", cycle.AuditCertificateHash);
    }

    [Fact]
    public async Task BR_IOT_03_GetCycles_Returns_Historical_Runs()
    {
        // Act
        var response = await _client.GetAsync("/api/sterilization/cycles");

        // Assert
        response.EnsureSuccessStatusCode();
        var cycles = await response.Content.ReadFromJsonAsync<List<SterilizationController.AutoclaveCycleDto>>(_jsonOptions);

        Assert.NotNull(cycles);
        Assert.True(cycles.Count >= 3);
        Assert.Contains(cycles, c => c.CycleId == "cycle-seed-01");
        Assert.Contains(cycles, c => c.CycleId == "cycle-seed-03" && c.CycleStatus == "Failed");
    }
}
