using System;
using System.Threading.Tasks;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/webauthn")]
public class WebAuthnController : ControllerBase
{
    private readonly ClinicDbContext _context;
    private readonly IJwtService _jwtService;

    public WebAuthnController(ClinicDbContext context, IJwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    [HttpPost("register/options")]
    [Authorize]
    public IActionResult GenerateRegisterOptions()
    {
        // Simulated FIDO2 attestation options generation (BR-PWA-02)
        return Ok(new
        {
            challenge = Guid.NewGuid().ToString("N"),
            rp = new { name = "Smart Clinic Management System", id = "cliniccorp.ai" },
            user = new { id = User.GetUserId(), name = User.Identity?.Name, displayName = User.Identity?.Name },
            pubKeyCredParams = new[] { new { type = "public-key", alg = -7 }, new { type = "public-key", alg = -257 } },
            authenticatorSelection = new { authenticatorAttachment = "platform", userVerification = "required" },
            timeout = 60000,
            attestation = "none"
        });
    }

    [HttpPost("register")]
    [Authorize]
    public async Task<IActionResult> RegisterCredential([FromBody] WebAuthnRegisterRequest request)
    {
        var userId = User.GetUserId();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return Unauthorized();

        var cred = new WebAuthnCredential
        {
            UserId = userId,
            CredentialId = Convert.FromBase64String(request.Id),
            PublicKey = Convert.FromBase64String(request.PublicKeyBase64),
            AuthenticatorAttachment = "platform",
            SignCount = 0
        };

        _context.WebAuthnCredentials.Add(cred);
        
        user.BiometricEnabled = true;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Biometric authenticator registered successfully" });
    }
}

public class WebAuthnRegisterRequest
{
    public string Id { get; set; } = string.Empty;
    public string PublicKeyBase64 { get; set; } = string.Empty;
}
