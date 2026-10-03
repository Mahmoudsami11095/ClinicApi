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

public class InwardShipmentIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public InwardShipmentIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private string GenerateJwtToken(string role = "doctor", string doctorId = "doc-test-1", string? clinicId = "clinic-1")
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes("SuperSecretTestingKeyThatIsAtLeast32BytesLong!");
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user-doc-mat-1"),
            new(ClaimTypes.Name, "Dr. Inventory Doctor"),
            new(ClaimTypes.Role, role),
            new("DoctorId", doctorId)
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
    public async Task REQ_INV_02_ReceiveInwardShipment_IncrementsStockAndUpdatesSupplierInfo()
    {
        const string matId = "mat-shipment-test-1";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
            if (!await db.Clinics.AnyAsync(c => c.Id == "clinic-1"))
            {
                db.Clinics.Add(new ClinicEntity { Id = "clinic-1", Name = "Clinic 1", CreatorDoctorId = "doc-test-1" });
            }
            if (!await db.Materials.AnyAsync(m => m.Id == matId))
            {
                db.Materials.Add(new Material
                {
                    Id = matId,
                    ClinicId = "clinic-1",
                    DoctorId = "doc-test-1",
                    Name = "Dental Anesthetic Articaine 4%",
                    Quantity = 15,
                    Unit = "Cartridges",
                    MinStockAlert = 10
                });
                await db.SaveChangesAsync();
            }
        }

        var token = GenerateJwtToken();

        var payload = new
        {
            QuantityReceived = 50,
            SupplierName = "MedTech Egypt Suppliers",
            PurchaseOrderRef = "PO-2026-ART-001",
            BatchNumber = "LOT-2026-ART",
            UnitCost = 45.00m,
            ExpirationDate = DateTime.UtcNow.AddYears(1)
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/materials/{matId}/inward-shipment")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            )
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");

        Assert.Equal(65, data.GetProperty("quantity").GetInt32()); // 15 + 50 = 65
        Assert.Equal("MedTech Egypt Suppliers", data.GetProperty("supplierName").GetString());
        Assert.Equal("PO-2026-ART-001", data.GetProperty("purchaseOrderRef").GetString());
        Assert.Equal(45.00m, data.GetProperty("unitCost").GetDecimal());
        Assert.NotNull(data.GetProperty("lastRestockedAt").GetString());
    }
}
