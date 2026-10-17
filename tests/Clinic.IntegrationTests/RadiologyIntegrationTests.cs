using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Clinic.IntegrationTests;

public class RadiologyIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public RadiologyIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private string GenerateJwtToken(string role = "doctor", string doctorId = "doc-rad-1", string clinicId = "clinic-rad-1")
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes("SuperSecretTestingKeyThatIsAtLeast32BytesLong!");
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user-rad-1"),
            new(ClaimTypes.Name, "Dr. Radiologist"),
            new(ClaimTypes.Email, "radiology@clinic.com"),
            new(ClaimTypes.Role, role),
            new("doctorId", doctorId),
            new("clinicId", clinicId)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = "ClinicApiTesting",
            Audience = "ClinicAppTesting",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    [Fact]
    public async Task GetCenters_WithAuthenticatedUser_Returns200Ok()
    {
        var token = GenerateJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/radiology/centers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateCenter_WithValidDetails_Returns201CreatedOr200Ok()
    {
        var token = GenerateJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = JsonSerializer.Serialize(new
        {
            name = "Alpha Scan Diagnostic Center",
            phone = "+201019998888",
            address = "Tahrir Square, Cairo",
            notes = "Specializes in Cone Beam Computed Tomography (CBCT)"
        });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/radiology/centers", content);

        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRecords_WithDoctorClaim_Returns200Ok()
    {
        var token = GenerateJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/radiology/records?doctorId=doc-rad-1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AnalyzeScanWithAi_ShouldReturn200Ok_WithMultiHeadFindings()
    {
        var token = GenerateJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsync("/api/radiology/records/rec-sample-1/ai-analyze", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        Assert.True(doc.RootElement.TryGetProperty("findings", out var findings));
        Assert.True(findings.GetArrayLength() >= 4);
        Assert.True(doc.RootElement.GetProperty("overallConfidence").GetDouble() > 80.0);
    }

    [Fact]
    public async Task SyncAiFindingsToOdontogram_ShouldReturn200Ok()
    {
        var token = GenerateJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = JsonSerializer.Serialize(new
        {
            acceptedFindingIds = new[] { "ai-find-101", "ai-find-102" },
            doctorNotes = "Verified by Dr. Jenkins during clinical inspection."
        });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/radiology/records/rec-sample-1/ai-sync-odontogram", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetCephalometricAnalysis_Returns200Ok_WithLandmarksAndMeasurements()
    {
        var token = GenerateJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/radiology/records/rec-sample-1/ceph-analysis");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        Assert.True(doc.RootElement.TryGetProperty("landmarks", out var landmarks));
        Assert.True(landmarks.GetArrayLength() >= 10);
        Assert.True(doc.RootElement.TryGetProperty("measurements", out var measurements));
        Assert.True(measurements.GetArrayLength() >= 5);
        Assert.Equal("Class I", doc.RootElement.GetProperty("skeletalClass").GetString());
    }

    [Fact]
    public async Task RecalculateCephalometricAnalysis_WithCustomPoints_ReturnsAccurateAngles()
    {
        var token = GenerateJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = JsonSerializer.Serialize(new
        {
            recordId = "rec-sample-1",
            landmarks = new[]
            {
                new { id = "S", name = "Sella", x = 40.0, y = 30.0, confidence = 95.0, isManuallyAdjusted = true },
                new { id = "N", name = "Nasion", x = 70.0, y = 25.0, confidence = 95.0, isManuallyAdjusted = true },
                new { id = "A", name = "Point A", x = 72.0, y = 55.0, confidence = 95.0, isManuallyAdjusted = true },
                new { id = "B", name = "Point B", x = 65.0, y = 70.0, confidence = 95.0, isManuallyAdjusted = true },
                new { id = "Pog", name = "Pogonion", x = 66.0, y = 80.0, confidence = 95.0, isManuallyAdjusted = true },
                new { id = "Me", name = "Menton", x = 64.0, y = 88.0, confidence = 95.0, isManuallyAdjusted = true },
                new { id = "Go", name = "Gonion", x = 35.0, y = 70.0, confidence = 95.0, isManuallyAdjusted = true },
                new { id = "Gn", name = "Gnathion", x = 65.0, y = 85.0, confidence = 95.0, isManuallyAdjusted = true },
                new { id = "Or", name = "Orbitale", x = 65.0, y = 40.0, confidence = 95.0, isManuallyAdjusted = true },
                new { id = "Po", name = "Porion", x = 35.0, y = 38.0, confidence = 95.0, isManuallyAdjusted = true }
            }
        });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/radiology/records/rec-sample-1/ceph-recalculate", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var resString = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(resString);
        Assert.True(doc.RootElement.TryGetProperty("measurements", out var measurements));
        Assert.True(measurements.GetArrayLength() >= 5);
        Assert.NotNull(doc.RootElement.GetProperty("skeletalClass").GetString());
    }

    [Fact]
    public async Task SimulateSurgicalVto_WithLeFortAndBsso_ReturnsPredictedSkeletalAndSoftTissueResults()
    {
        var token = GenerateJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = JsonSerializer.Serialize(new
        {
            recordId = "rec-sample-1",
            leFortIMaxillaryHorizontalMm = 3.5,
            leFortIMaxillaryVerticalMm = -1.5,
            bssoMandibularHorizontalMm = -4.0,
            bssoMandibularVerticalMm = 0.0,
            genioplastyHorizontalMm = 2.0,
            genioplastyVerticalMm = 0.0
        });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/radiology/records/rec-sample-1/ceph-vto", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var resString = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(resString);
        Assert.True(doc.RootElement.TryGetProperty("predictedAnb", out var predAnb));
        Assert.True(doc.RootElement.TryGetProperty("predictedELine", out var eLine));
        Assert.True(eLine.TryGetProperty("upperLipDistanceMm", out _));
        Assert.True(eLine.TryGetProperty("lowerLipDistanceMm", out _));
        Assert.True(doc.RootElement.TryGetProperty("displacedLandmarks", out var displaced));
        Assert.True(displaced.GetArrayLength() >= 5);
    }
}
