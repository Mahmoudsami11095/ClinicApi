using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Clinic.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user, List<string>? clinicIds = null)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]!));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Role, user.Role.ToString().ToLower()),
            new("title", user.Title ?? "")
        };

        if (user.ClinicId != null)
            claims.Add(new Claim("clinicId", user.ClinicId));

        if (user.DoctorId != null)
            claims.Add(new Claim("doctorId", user.DoctorId));

        if (user.PatientId != null)
            claims.Add(new Claim("patientId", user.PatientId));

        if (clinicIds != null)
        {
            foreach (var cid in clinicIds)
                claims.Add(new Claim("clinicIds", cid));
        }

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(
                double.Parse(_configuration["Jwt:ExpiryHours"] ?? "24")),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
