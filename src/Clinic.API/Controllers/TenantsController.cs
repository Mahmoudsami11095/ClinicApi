using System;
using System.Threading.Tasks;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/tenants")]
[AllowAnonymous]
public class TenantsController : ControllerBase
{
    private readonly ClinicDbContext _context;

    public TenantsController(ClinicDbContext context)
    {
        _context = context;
    }

    [HttpPost("onboard")]
    public async Task<IActionResult> OnboardTenant([FromBody] OnboardTenantRequest request)
    {
        // 1. Create Tenant (BR-SAAS-01)
        var tenant = new Tenant
        {
            OrganizationName = request.OrganizationName,
            Subdomain = request.Subdomain.ToLower().Trim(),
            SubscriptionTier = request.SubscriptionTier,
            CreatedAt = DateTime.UtcNow
        };
        _context.Tenants.Add(tenant);

        // 2. Create HQ Clinic for Tenant
        var clinic = new ClinicEntity
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenant.Id,
            Name = $"{request.OrganizationName} HQ",
            Address = "Headquarters",
            PhoneNumber = "0000000000"
        };
        _context.Clinics.Add(clinic);

        // 3. Create Tenant Admin
        var admin = new User
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenant.Id,
            ClinicId = clinic.Id,
            Name = request.AdminName,
            Email = request.AdminEmail,
            PasswordHash = "$2a$11$SimulatedHashForDemo123456", // BCrypt simulation
            Role = Domain.Enums.UserRole.Admin
        };
        _context.Users.Add(admin);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Tenant organization onboarded successfully.",
            tenantId = tenant.Id,
            subdomain = tenant.Subdomain,
            loginUrl = $"https://{tenant.Subdomain}.cliniccorp.ai/login"
        });
    }

    [HttpGet("resolve/{subdomain}")]
    public async Task<IActionResult> ResolveTenant(string subdomain)
    {
        var tenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Subdomain == subdomain.ToLower().Trim());

        if (tenant == null || !tenant.IsActive) return NotFound();

        return Ok(new
        {
            tenantId = tenant.Id,
            name = tenant.OrganizationName,
            logo = tenant.LogoUrl,
            color = tenant.PrimaryColorHex
        });
    }
}

public class OnboardTenantRequest
{
    public string OrganizationName { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string SubscriptionTier { get; set; } = "Starter";
    public string AdminName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
}
