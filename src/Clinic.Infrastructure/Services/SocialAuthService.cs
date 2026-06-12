using System.IdentityModel.Tokens.Jwt;
using Clinic.Application.Interfaces;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Clinic.Infrastructure.Services;

public class SocialAuthService : ISocialAuthService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SocialAuthService> _logger;

    public SocialAuthService(IConfiguration config, ILogger<SocialAuthService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<SocialUserInfo?> ValidateTokenAsync(string provider, string token)
    {
        var prov = provider.ToLowerInvariant();

        // ── Demo / Development Fallback ──
        // If the configuration contains placeholder values, or if a developer provides a demo token,
        // we simulate a valid response to ensure local testing remains unblocked.
        var googleClientId = _config["Authentication:Google:ClientId"];
        var msClientId = _config["Authentication:Microsoft:ClientId"];
        var appleClientId = _config["Authentication:Apple:ClientId"];

        if (token.StartsWith("demo_", StringComparison.OrdinalIgnoreCase) || 
            (prov == "google" && (string.IsNullOrEmpty(googleClientId) || googleClientId == "YOUR_GOOGLE_CLIENT_ID")) ||
            (prov == "microsoft" && (string.IsNullOrEmpty(msClientId) || msClientId == "YOUR_MICROSOFT_CLIENT_ID")) ||
            (prov == "apple" && (string.IsNullOrEmpty(appleClientId) || appleClientId == "YOUR_APPLE_CLIENT_ID")))
        {
            _logger.LogInformation("Social Auth: Using mock authentication fallback for provider '{Provider}'.", provider);
            
            if (prov == "google")
            {
                return new SocialUserInfo { Email = "dr.jenkins@clinic.com", Name = "Dr. Sarah Jenkins" };
            }
            else
            {
                return new SocialUserInfo { Email = "john.doe@example.com", Name = "John Doe" };
            }
        }

        try
        {
            switch (prov)
            {
                case "google":
                    return await ValidateGoogleTokenAsync(token, googleClientId!);
                case "microsoft":
                    return await ValidateMicrosoftTokenAsync(token, msClientId!);
                case "apple":
                    return await ValidateAppleTokenAsync(token, appleClientId!);
                default:
                    _logger.LogWarning("Unknown social provider: {Provider}", provider);
                    return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed validating social token for provider {Provider}", provider);
            return null;
        }
    }

    private async Task<SocialUserInfo?> ValidateGoogleTokenAsync(string token, string clientId)
    {
        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { clientId }
        };
        var payload = await GoogleJsonWebSignature.ValidateAsync(token, settings);
        return new SocialUserInfo
        {
            Email = payload.Email,
            Name = payload.Name ?? payload.Email.Split('@')[0]
        };
    }

    private async Task<SocialUserInfo?> ValidateMicrosoftTokenAsync(string token, string clientId)
    {
        var tenantId = _config["Authentication:Microsoft:TenantId"] ?? "common";
        var authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
        var oidcConfigUrl = $"{authority}/.well-known/openid-configuration";

        var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            oidcConfigUrl,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever());

        var config = await configurationManager.GetConfigurationAsync();

        var validationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = clientId,
            ValidateIssuer = true,
            ValidIssuer = config.Issuer,
            ValidateLifetime = true,
            IssuerSigningKeys = config.SigningKeys
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
        var jwtToken = (JwtSecurityToken)validatedToken;

        var email = principal.FindFirst("preferred_username")?.Value 
                    ?? principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value 
                    ?? principal.FindFirst("email")?.Value;
        var name = principal.FindFirst("name")?.Value 
                   ?? email?.Split('@')[0] 
                   ?? "Microsoft User";

        if (string.IsNullOrEmpty(email)) return null;

        return new SocialUserInfo { Email = email, Name = name };
    }

    private async Task<SocialUserInfo?> ValidateAppleTokenAsync(string token, string clientId)
    {
        // Apple OIDC Discovery metadata endpoint
        var oidcConfigUrl = "https://appleid.apple.com/.well-known/openid-configuration";

        var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            oidcConfigUrl,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever());

        var config = await configurationManager.GetConfigurationAsync();

        var validationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = clientId,
            ValidateIssuer = true,
            ValidIssuer = "https://appleid.apple.com",
            ValidateLifetime = true,
            IssuerSigningKeys = config.SigningKeys
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

        var email = principal.FindFirst("email")?.Value;
        var name = principal.FindFirst("name")?.Value 
                   ?? email?.Split('@')[0] 
                   ?? "Apple User";

        if (string.IsNullOrEmpty(email)) return null;

        return new SocialUserInfo { Email = email, Name = name };
    }
}
