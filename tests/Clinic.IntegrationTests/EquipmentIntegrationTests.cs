using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Clinic.Application.DTOs;
using Clinic.Domain.Entities;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Clinic.IntegrationTests;

public class EquipmentIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public EquipmentIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private string GenerateJwtToken(string role = "doctor", string userId = "usr-eq-1", string clinicId = "clinic-eq-test")
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes("SuperSecretTestingKeyThatIsAtLeast32BytesLong!");
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, "Dr. Equipment Specialist"),
            new(ClaimTypes.Email, "equipment.test@clinic.com"),
            new(ClaimTypes.Role, role),
            new("clinicId", clinicId)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(2),
            Issuer = "ClinicApiTesting",
            Audience = "ClinicAppTesting",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    [Fact]
    public async Task GetEquipment_WithoutToken_Returns401Unauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/equipment");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Equipment_FullCrudLifecycle_ExecutesSuccessfully()
    {
        // Arrange
        var clinicId = "clinic-crud-" + Guid.NewGuid().ToString("N")[..8];
        var token = GenerateJwtToken(clinicId: clinicId);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/equipment");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var newDevice = new EquipmentDto
        {
            ClinicId = clinicId,
            Name = "Sirona Heliodent Plus Intraoral X-Ray",
            Category = "Diagnostic",
            ModelNumber = "Heliodent Plus",
            SerialNumber = "SN-XRAY-4491",
            Manufacturer = "Dentsply Sirona",
            RoomOrChair = "Radiology Bay 1",
            PurchaseCost = 6500.00m,
            PurchaseDate = DateTime.UtcNow.AddMonths(-6),
            Status = "Operational"
        };
        request.Content = JsonContent.Create(newDevice);

        // 1. CREATE
        var createResponse = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var createContent = await createResponse.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createContent);
        var createdId = createDoc.RootElement.GetProperty("data").GetProperty("id").GetString();
        Assert.NotNull(createdId);

        // 2. GET BY ID
        var getReq = new HttpRequestMessage(HttpMethod.Get, $"/api/equipment/{createdId}");
        getReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var getResponse = await _client.SendAsync(getReq);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var getContent = await getResponse.Content.ReadAsStringAsync();
        using var getDoc = JsonDocument.Parse(getContent);
        var retrievedName = getDoc.RootElement.GetProperty("data").GetProperty("name").GetString();
        Assert.Equal("Sirona Heliodent Plus Intraoral X-Ray", retrievedName);

        // 3. UPDATE
        var updateReq = new HttpRequestMessage(HttpMethod.Put, $"/api/equipment/{createdId}");
        updateReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        newDevice.Id = createdId;
        newDevice.RoomOrChair = "Radiology Bay 2 (Relocated)";
        newDevice.Status = "Maintenance Due";
        updateReq.Content = JsonContent.Create(newDevice);

        var updateResponse = await _client.SendAsync(updateReq);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // 4. LOG MAINTENANCE
        var maintReq = new HttpRequestMessage(HttpMethod.Post, $"/api/equipment/{createdId}/maintenance");
        maintReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var logPayload = new EquipmentMaintenanceLogRequest
        {
            ServiceDate = DateTime.UtcNow,
            NextDueDate = DateTime.UtcNow.AddMonths(6),
            Notes = "Radiation leakage survey completed with 0 mR/hr detected. Collimator aligned.",
            ServiceProvider = "Sirona Authorized Technical Services",
            ServiceContactPhone = "+1 800 555 1234",
            Status = "Operational"
        };
        maintReq.Content = JsonContent.Create(logPayload);

        var maintResponse = await _client.SendAsync(maintReq);
        Assert.Equal(HttpStatusCode.OK, maintResponse.StatusCode);

        var maintContent = await maintResponse.Content.ReadAsStringAsync();
        using var maintDoc = JsonDocument.Parse(maintContent);
        var updatedStatus = maintDoc.RootElement.GetProperty("data").GetProperty("status").GetString();
        var notes = maintDoc.RootElement.GetProperty("data").GetProperty("maintenanceNotes").GetString();
        Assert.Equal("Operational", updatedStatus);
        Assert.Contains("Radiation leakage survey", notes);

        // 5. DELETE
        var deleteReq = new HttpRequestMessage(HttpMethod.Delete, $"/api/equipment/{createdId}");
        deleteReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var deleteResponse = await _client.SendAsync(deleteReq);
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        // 6. VERIFY NOT FOUND AFTER DELETE
        var verifyReq = new HttpRequestMessage(HttpMethod.Get, $"/api/equipment/{createdId}");
        verifyReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var verifyResponse = await _client.SendAsync(verifyReq);
        Assert.Equal(HttpStatusCode.NotFound, verifyResponse.StatusCode);
    }

    [Fact]
    public async Task SeedDefaults_PopulatesFiveStandardDentalAssets()
    {
        // Arrange
        var clinicId = "clinic-seed-" + Guid.NewGuid().ToString("N")[..8];
        var token = GenerateJwtToken(clinicId: clinicId);

        var seedReq = new HttpRequestMessage(HttpMethod.Post, $"/api/equipment/seed-defaults?clinicId={clinicId}");
        seedReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        seedReq.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.SendAsync(seedReq);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var dataArray = doc.RootElement.GetProperty("data");
        Assert.Equal(5, dataArray.GetArrayLength());

        // Verify querying /api/equipment returns the 5 items
        var listReq = new HttpRequestMessage(HttpMethod.Get, $"/api/equipment?clinicId={clinicId}");
        listReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var listResponse = await _client.SendAsync(listReq);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var listContent = await listResponse.Content.ReadAsStringAsync();
        using var listDoc = JsonDocument.Parse(listContent);
        var items = listDoc.RootElement.GetProperty("data");
        Assert.Equal(5, items.GetArrayLength());
    }

    [Fact]
    public async Task Equipment_CrossClinicAccess_ReturnsForbidden()
    {
        // Arrange
        var clinicA = "clinic-A-" + Guid.NewGuid().ToString("N")[..8];
        var clinicB = "clinic-B-" + Guid.NewGuid().ToString("N")[..8];

        var tokenA = GenerateJwtToken(clinicId: clinicA);
        var tokenB = GenerateJwtToken(clinicId: clinicB);

        // Create equipment in Clinic A
        var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/equipment");
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        createReq.Content = JsonContent.Create(new EquipmentDto
        {
            ClinicId = clinicA,
            Name = "Exclusive Clinic A Autoclave",
            Category = "Sterilization"
        });

        var createRes = await _client.SendAsync(createReq);
        Assert.Equal(HttpStatusCode.OK, createRes.StatusCode);
        var createDoc = JsonDocument.Parse(await createRes.Content.ReadAsStringAsync());
        var idA = createDoc.RootElement.GetProperty("data").GetProperty("id").GetString();

        // Act: User from Clinic B attempts to view item from Clinic A
        var getReq = new HttpRequestMessage(HttpMethod.Get, $"/api/equipment/{idA}");
        getReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var getRes = await _client.SendAsync(getReq);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, getRes.StatusCode);
    }
}
