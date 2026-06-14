namespace Clinic.Application.Interfaces;

public interface IWhatsAppOtpService
{
    /// <summary>
    /// Generates a 6-digit OTP, stores it with 5-minute expiration, and sends it via OpenWA API.
    /// Incorporates rate-limiting to prevent spam.
    /// </summary>
    Task<(bool Success, string Message)> RequestOtpAsync(string phoneNumber);

    /// <summary>
    /// Verifies if the OTP matches the stored one for the phone number.
    /// </summary>
    bool VerifyOtp(string phoneNumber, string code);

    /// <summary>
    /// Removes the OTP once verified.
    /// </summary>
    void RemoveOtp(string phoneNumber);
}
