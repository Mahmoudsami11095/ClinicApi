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
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        return Ok(new
        {
            challenge = Guid.NewGuid().ToString("N"),
            rp = new { name = "Smart Clinic Management System", id = "cliniccorp.ai" },
            user = new { id = userId, name = User.Identity?.Name, displayName = User.Identity?.Name },
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
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
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

    [HttpPost("authenticate/options")]
    [AllowAnonymous]
    public IActionResult GenerateAuthenticateOptions()
    {
        return Ok(new
        {
            challenge = Guid.NewGuid().ToString("N"),
            timeout = 60000,
            userVerification = "required",
            rpId = "cliniccorp.ai"
        });
    }

    [HttpPost("authenticate")]
    [AllowAnonymous]
    public async Task<IActionResult> AuthenticateCredential([FromBody] WebAuthnAuthenticateRequest request)
    {
        var credIdBytes = Convert.FromBase64String(request.Id);
        var cred = await _context.WebAuthnCredentials
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.CredentialId == credIdBytes);

        if (cred == null || cred.User == null || !cred.User.BiometricEnabled)
        {
            return Unauthorized(new { message = "Invalid biometric credential" });
        }

        // Simulate FIDO2 signature verification success
        var token = _jwtService.GenerateToken(cred.User);
        return Ok(new { token, user = cred.User });
    }
}

public class WebAuthnRegisterRequest
{
    public string Id { get; set; } = string.Empty;
    public string PublicKeyBase64 { get; set; } = string.Empty;
}

public class WebAuthnAuthenticateRequest
{
    public string Id { get; set; } = string.Empty;
    public string AuthenticatorData { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public string ClientDataJson { get; set; } = string.Empty;
}
