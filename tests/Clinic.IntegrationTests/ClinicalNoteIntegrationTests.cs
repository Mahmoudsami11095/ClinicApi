using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Clinic.Application.DTOs;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Clinic.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace Clinic.IntegrationTests;

public class ClinicalNoteIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ClinicalNoteIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private string GenerateJwtToken(string doctorId = "doc-test-1", string role = "doctor", string? clinicId = "clinic-1")
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes("SuperSecretTestingKeyThatIsAtLeast32BytesLong!");
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user-test-1"),
            new(ClaimTypes.Name, "Dr. Test Doctor"),
            new(ClaimTypes.Email, "doctor@example.com"),
            new(ClaimTypes.Role, role),
            new("doctorId", doctorId)
        };
        if (!string.IsNullOrEmpty(clinicId))
        {
            claims.Add(new("clinicId", clinicId));
        }

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
    public async Task BR_RX_03_DeleteEndpoint_IsExplicitlyBlocked_ReturnsBadRequest()
    {
        // Arrange
        var token = GenerateJwtToken();
        var request = new HttpRequestMessage(HttpMethod.Delete, "/api/clinical-notes/note-999");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert - BR-RX-03 / BR-MED-01: DELETE is permanently forbidden
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Clinical encounter notes cannot be deleted", body);
        Assert.Contains("permanent and immutable", body);
    }

    [Fact]
    public async Task BR_RX_03_Repository_DeleteAsync_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(databaseName: "RepoTest_" + Guid.NewGuid().ToString())
            .Options;

        await using var context = new ClinicDbContext(options);
        var repo = new ClinicalNoteRepository(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => repo.DeleteAsync("note-test-id"));
        Assert.Contains("BR-RX-03 / BR-MED-01", ex.Message);
        Assert.Contains("Clinical encounter notes cannot be deleted", ex.Message);
    }

    [Fact]
    public async Task BR_RX_03_CreateAndAmend_PreservesOriginalNote_AndAppendsAmendment()
    {
        // Arrange - Seed clinic and patient
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
            if (!await db.Clinics.AnyAsync(c => c.Id == "clinic-1"))
            {
                db.Clinics.Add(new ClinicEntity
                {
                    Id = "clinic-1",
                    Name = "Test Dental Clinic",
                    CreatorDoctorId = "doc-test-1"
                });
            }
            if (!await db.Patients.AnyAsync(p => p.Id == "pat-note-01"))
            {
                db.Patients.Add(new Patient
                {
                    Id = "pat-note-01",
                    FirstName = "Karim",
                    LastName = "Nabil",
                    ClinicId = "clinic-1",
                    Gender = "Male",
                    DateOfBirth = "1988-03-20"
                });
            }
            await db.SaveChangesAsync();
        }

        var token = GenerateJwtToken();

        // 1. Create original note
        var createDto = new CreateClinicalNoteDto
        {
            PatientId = "pat-note-01",
            Title = "Periodontal Examination",
            Category = "Consultation",
            Notes = "Moderate plaque accumulation with localized gingival inflammation around lower incisors."
        };

        var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/clinical-notes")
        {
            Content = new StringContent(JsonSerializer.Serialize(createDto), Encoding.UTF8, "application/json")
        };
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createRes = await _client.SendAsync(createReq);
        Assert.Equal(HttpStatusCode.OK, createRes.StatusCode);

        var createJson = await createRes.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createJson);
        var createdNoteId = createDoc.RootElement.GetProperty("data").GetProperty("id").GetString();
        Assert.NotNull(createdNoteId);

        // 2. Amend the note
        var amendDto = new AmendClinicalNoteDto
        {
            AmendedText = "Addendum: Prescribed 0.12% Chlorhexidine gluconate mouthwash BID for 14 days.",
            Reason = "Follow-up prescription addition"
        };

        var amendReq = new HttpRequestMessage(HttpMethod.Put, $"/api/clinical-notes/{createdNoteId}")
        {
            Content = new StringContent(JsonSerializer.Serialize(amendDto), Encoding.UTF8, "application/json")
        };
        amendReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var amendRes = await _client.SendAsync(amendReq);
        Assert.Equal(HttpStatusCode.OK, amendRes.StatusCode);

        // 3. Verify via GET that original note is unchanged and amendment is present
        var getReq = new HttpRequestMessage(HttpMethod.Get, $"/api/clinical-notes/{createdNoteId}");
        getReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var getRes = await _client.SendAsync(getReq);
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);

        var getJson = await getRes.Content.ReadAsStringAsync();
        using var getDoc = JsonDocument.Parse(getJson);
        var data = getDoc.RootElement.GetProperty("data");

        // Assert original note is untouched
        Assert.Equal("Moderate plaque accumulation with localized gingival inflammation around lower incisors.", data.GetProperty("notes").GetString());

        // Assert amendment trail
        var amendments = data.GetProperty("amendments");
        Assert.Equal(1, amendments.GetArrayLength());
        Assert.Equal("Addendum: Prescribed 0.12% Chlorhexidine gluconate mouthwash BID for 14 days.", amendments[0].GetProperty("amendedText").GetString());
        Assert.Equal("Follow-up prescription addition", amendments[0].GetProperty("reason").GetString());
        Assert.Equal("doc-test-1", amendments[0].GetProperty("authorId").GetString());
        Assert.Equal("Dr. Test Doctor", amendments[0].GetProperty("authorName").GetString());
    }

    [Fact]
    public async Task UAT_SEC_01_ReceptionistOrAssistant_CannotAccess_ClinicalNotes_ReturnsForbidden()
    {
        // Arrange - Assistant / Receptionist token
        var assistantToken = GenerateJwtToken(doctorId: "", role: "assistant", clinicId: "clinic-1");

        // Act - Attempt to query confidential clinical encounter notes
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/clinical-notes?patientId=pat-note-01");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", assistantToken);

        var response = await _client.SendAsync(request);

        // Assert - REQ-SEC-01 / UAT-SEC-01: Front desk access denied
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UAT_SEC_01_ReceptionistOrAssistant_CannotCreate_ClinicalNotes_ReturnsForbidden()
    {
        // Arrange - Assistant / Receptionist token
        var assistantToken = GenerateJwtToken(doctorId: "", role: "assistant", clinicId: "clinic-1");

        var createDto = new CreateClinicalNoteDto
        {
            PatientId = "pat-note-01",
            Title = "Unauthorized Entry",
            Notes = "Receptionist attempting clinical entry"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/clinical-notes")
        {
            Content = new StringContent(JsonSerializer.Serialize(createDto), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", assistantToken);

        var response = await _client.SendAsync(request);

        // Assert - REQ-SEC-01 / UAT-SEC-01: Front desk cannot record clinical notes
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
