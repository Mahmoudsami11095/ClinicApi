using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace Clinic.IntegrationTests;

public class ClinicRoomsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ClinicRoomsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private string GenerateJwtToken(string role = "doctor", string doctorId = "doc-room-1", string? clinicId = "clinic-branch-east")
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes("SuperSecretTestingKeyThatIsAtLeast32BytesLong!");
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user-doc-room-1"),
            new(ClaimTypes.Name, "Dr. Room Specialist"),
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
    public async Task REQ_CLI_03_StartConsultation_AssignsExaminationRoom()
    {
        const string clinicId = "clinic-branch-east";
        const string docId = "doc-room-1";
        const string patId = "pat-room-1";
        const string apptId = "apt-room-test-1";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
            if (!await db.Clinics.AnyAsync(c => c.Id == clinicId))
            {
                db.Clinics.Add(new ClinicEntity
                {
                    Id = clinicId,
                    Name = "East Branch Clinic",
                    BranchCode = "EAST-01",
                    Rooms = "Dental Chair 1,Dental Chair 2,VIP Suite",
                    CreatorDoctorId = docId
                });
            }
            if (!await db.Patients.AnyAsync(p => p.Id == patId))
            {
                db.Patients.Add(new Patient
                {
                    Id = patId,
                    FirstName = "Ayman",
                    LastName = "Shawky",
                    ClinicId = clinicId,
                    Gender = "Male"
                });
            }
            if (!await db.Appointments.AnyAsync(a => a.Id == apptId))
            {
                db.Appointments.Add(new Appointment
                {
                    Id = apptId,
                    ClinicId = clinicId,
                    DoctorId = docId,
                    PatientId = patId,
                    Date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"),
                    Status = "waiting",
                    QueueNumber = 3,
                    ArrivedAt = DateTime.UtcNow.AddMinutes(-10).ToString("o")
                });
            }
            await db.SaveChangesAsync();
        }

        var token = GenerateJwtToken(role: "doctor", doctorId: docId, clinicId: clinicId);

        // Act - Call patient into Dental Chair 2
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/appointments/{apptId}/start-consultation?roomNumber=Dental%20Chair%202");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");

        Assert.Equal("in_consultation", data.GetProperty("status").GetString());
        Assert.Equal("Dental Chair 2", data.GetProperty("roomNumber").GetString());
    }
}
