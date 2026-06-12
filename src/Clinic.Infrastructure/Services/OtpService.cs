using System.Collections.Concurrent;
using Clinic.Application.Interfaces;

namespace Clinic.Infrastructure.Services;

public class OtpService : IOtpService
{
    // In-memory store for OTPs – suitable for demo/development.
    // Replace with Redis or database for production.
    private static readonly ConcurrentDictionary<string, (string Code, DateTime Expiry)> _otpStore = new();

    public string GenerateOtp(string email)
    {
        var code = Random.Shared.Next(100000, 999999).ToString();
        var key = email.ToLowerInvariant();
        _otpStore[key] = (code, DateTime.UtcNow.AddMinutes(10));
        Console.WriteLine($"[OTP] Generated Code for {email}: {code}");
        return code;
    }

    public bool VerifyOtp(string email, string code)
    {
        var key = email.ToLowerInvariant();
        if (_otpStore.TryGetValue(key, out var entry))
        {
            return entry.Code == code && entry.Expiry > DateTime.UtcNow;
        }
        return false;
    }

    public void RemoveOtp(string email)
    {
        var key = email.ToLowerInvariant();
        _otpStore.TryRemove(key, out _);
    }
}
