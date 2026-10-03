using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Clinic.Application.DTOs;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace Clinic.IntegrationTests;

public class PrescriptionImmutabilityIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public PrescriptionImmutabilityIntegrationTests(CustomWebApplicationFactory factory)
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
    public async Task BR_RX_02_FinalizedPrescription_UpdateAttempt_ReturnsBadRequest()
    {
        // Arrange - Seed clinic, patient, appointment, and finalized prescription
        const string rxId = "rx-finalized-test-1";
        const string apptId = "appt-rx-test-1";
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
            if (!await db.Clinics.AnyAsync(c => c.Id == "clinic-1"))
            {
                db.Clinics.Add(new ClinicEntity { Id = "clinic-1", Name = "Clinic 1", CreatorDoctorId = "doc-test-1" });
            }
            if (!await db.Appointments.AnyAsync(a => a.Id == apptId))
            {
                db.Appointments.Add(new Appointment
                {
                    Id = apptId,
                    ClinicId = "clinic-1",
                    PatientId = "pat-1",
                    DoctorId = "doc-test-1",
                    Date = "2026-10-03",
                    Status = "completed"
                });
            }
            if (!await db.Prescriptions.AnyAsync(p => p.Id == rxId))
            {
                db.Prescriptions.Add(new Prescription
                {
                    Id = rxId,
                    AppointmentId = apptId,
                    PatientId = "pat-1",
                    DoctorId = "doc-test-1",
                    Date = "2026-10-03",
                    IsFinalized = true,
                    Status = "finalized",
                    FinalizedAt = "2026-10-03T10:00:00Z",
                    DigitalSignature = "Digitally Signed by Dr. Test Doctor",
                    Medications = new List<MedicationItem>
                    {
                        new() { Name = "Amoxicillin 500mg", Dosage = "1 cap", Frequency = "TID", Duration = "7 days" }
                    }
                });
            }
            await db.SaveChangesAsync();
        }

        var token = GenerateJwtToken();

        // Act - Attempt PUT update on finalized prescription
        var updateDto = new PrescriptionDto
        {
            Id = rxId,
            AppointmentId = apptId,
            PatientId = "pat-1",
            DoctorId = "doc-test-1",
            Date = "2026-10-03",
            Medications = new List<MedicationItemDto>
            {
                new() { Name = "Ciprofloxacin 500mg", Dosage = "1 tab", Frequency = "BID", Duration = "5 days" }
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/prescriptions/{rxId}")
        {
            Content = new StringContent(JsonSerializer.Serialize(updateDto), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        // Assert - BR-RX-02: Edit is forbidden
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("BR-RX-02", body);
        Assert.Contains("legally locked and read-only", body);
    }

    [Fact]
    public async Task BR_RX_02_FinalizedPrescription_DeleteAttempt_ReturnsBadRequest()
    {
        // Arrange
        const string rxId = "rx-finalized-delete-test";
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
            if (!await db.Prescriptions.AnyAsync(p => p.Id == rxId))
            {
                db.Prescriptions.Add(new Prescription
                {
                    Id = rxId,
                    AppointmentId = "appt-del-test",
                    PatientId = "pat-1",
                    DoctorId = "doc-test-1",
                    Date = "2026-10-03",
                    IsFinalized = true,
                    Status = "finalized",
                    Medications = new List<MedicationItem>
                    {
                        new() { Name = "Augmentin", Dosage = "1g", Frequency = "BID", Duration = "5d" }
                    }
                });
                await db.SaveChangesAsync();
            }
        }

        var token = GenerateJwtToken();

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/prescriptions/{rxId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert - Deletion of finalized Rx is strictly forbidden
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("BR-RX-02", body);
        Assert.Contains("cannot be deleted", body);
    }

    [Fact]
    public async Task BR_RX_02_FinalizeEndpoint_DigitallyLocksPrescription()
    {
        // Arrange - Seed draft prescription
        const string draftRxId = "rx-draft-to-finalize";
        const string apptId = "appt-rx-test-2";
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
            if (!await db.Appointments.AnyAsync(a => a.Id == apptId))
            {
                db.Appointments.Add(new Appointment
                {
                    Id = apptId,
                    ClinicId = "clinic-1",
                    PatientId = "pat-1",
                    DoctorId = "doc-test-1",
                    Date = "2026-10-03",
                    Status = "completed"
                });
            }
            if (!await db.Prescriptions.AnyAsync(p => p.Id == draftRxId))
            {
                db.Prescriptions.Add(new Prescription
                {
                    Id = draftRxId,
                    AppointmentId = apptId,
                    PatientId = "pat-1",
                    DoctorId = "doc-test-1",
                    Date = "2026-10-03",
                    IsFinalized = false,
                    Status = "draft",
                    Medications = new List<MedicationItem>
                    {
                        new() { Name = "Paracetamol 500mg", Dosage = "1 tab", Frequency = "QID", Duration = "3 days" }
                    }
                });
            }
            await db.SaveChangesAsync();
        }

        var token = GenerateJwtToken();

        // Act - Call finalize endpoint
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/prescriptions/{draftRxId}/finalize")
        {
            Content = new StringContent(JsonSerializer.Serialize(new FinalizePrescriptionDto { DoctorName = "Dr. Test Doctor" }), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");

        Assert.True(data.GetProperty("isFinalized").GetBoolean());
        Assert.Equal("finalized", data.GetProperty("status").GetString());
        Assert.Contains("Digitally Signed by Dr. Test Doctor", data.GetProperty("digitalSignature").GetString());
        Assert.NotNull(data.GetProperty("finalizedAt").GetString());
    }

    [Fact]
    public async Task BR_RX_02_SupersedeEndpoint_ArchivesOriginalAndIssuesRevision()
    {
        // Arrange - Seed finalized prescription
        const string rxToSupersede = "rx-to-supersede-1";
        const string apptId = "appt-rx-test-3";
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
            if (!await db.Appointments.AnyAsync(a => a.Id == apptId))
            {
                db.Appointments.Add(new Appointment
                {
                    Id = apptId,
                    ClinicId = "clinic-1",
                    PatientId = "pat-1",
                    DoctorId = "doc-test-1",
                    Date = "2026-10-03",
                    Status = "completed"
                });
            }
            if (!await db.Prescriptions.AnyAsync(p => p.Id == rxToSupersede))
            {
                db.Prescriptions.Add(new Prescription
                {
                    Id = rxToSupersede,
                    AppointmentId = apptId,
                    PatientId = "pat-1",
                    DoctorId = "doc-test-1",
                    Date = "2026-10-03",
                    IsFinalized = true,
                    Status = "finalized",
                    FinalizedAt = "2026-10-03T09:00:00Z",
                    DigitalSignature = "Digitally Signed by Dr. Test Doctor",
                    Medications = new List<MedicationItem>
                    {
                        new() { Name = "Penicillin V 500mg", Dosage = "1 tab", Frequency = "QID", Duration = "7 days" }
                    }
                });
            }
            await db.SaveChangesAsync();
        }

        var token = GenerateJwtToken();

        // Act - Call supersede endpoint
        var supersedeDto = new SupersedePrescriptionDto
        {
            Reason = "Patient developed skin rash consistent with penicillin allergy. Switched to Clindamycin.",
            NewMedications = new List<MedicationItemDto>
            {
                new() { Name = "Clindamycin 300mg", Dosage = "1 cap", Frequency = "TID", Duration = "7 days" }
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/prescriptions/{rxToSupersede}/supersede")
        {
            Content = new StringContent(JsonSerializer.Serialize(supersedeDto), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");
        var original = doc.RootElement.GetProperty("original");

        // New prescription checks
        Assert.True(data.GetProperty("isFinalized").GetBoolean());
        Assert.Equal("finalized", data.GetProperty("status").GetString());
        Assert.Equal(rxToSupersede, data.GetProperty("supersedesPrescriptionId").GetString());
        var newMeds = data.GetProperty("medications");
        Assert.Equal(1, newMeds.GetArrayLength());
        Assert.Equal("Clindamycin 300mg", newMeds[0].GetProperty("name").GetString());

        // Original prescription checks
        Assert.Equal("superseded", original.GetProperty("status").GetString());
        Assert.Equal(data.GetProperty("id").GetString(), original.GetProperty("supersededById").GetString());
        Assert.Contains("penicillin allergy", original.GetProperty("supersedeReason").GetString());
    }
}
