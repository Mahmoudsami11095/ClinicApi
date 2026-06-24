namespace Clinic.Application.Interfaces;

public interface ISmsService
{
    Task<(bool Success, string Message)> SendSmsAsync(string phoneNumber, string message);
}
