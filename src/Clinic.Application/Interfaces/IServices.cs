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
