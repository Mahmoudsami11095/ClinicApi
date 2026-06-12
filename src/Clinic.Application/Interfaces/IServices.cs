using Clinic.Domain.Entities;

namespace Clinic.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user, List<string>? clinicIds = null);
}

public interface IOtpService
{
    string GenerateOtp(string email);
    bool VerifyOtp(string email, string code);
    void RemoveOtp(string email);
}

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body);
}

public class SocialUserInfo
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public interface ISocialAuthService
{
    Task<SocialUserInfo?> ValidateTokenAsync(string provider, string token);
}
