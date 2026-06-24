using Clinic.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Clinic.Infrastructure.Services;

public class TwilioSmsService : ISmsService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TwilioSmsService> _logger;

    public TwilioSmsService(IConfiguration configuration, ILogger<TwilioSmsService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(bool Success, string Message)> SendSmsAsync(string phoneNumber, string message)
    {
        try
        {
            var accountSid = _configuration["Twilio:AccountSid"];
            var authToken = _configuration["Twilio:AuthToken"];
            var fromPhoneNumber = _configuration["Twilio:FromPhoneNumber"];

            if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(fromPhoneNumber))
            {
                _logger.LogWarning("Twilio settings are missing in configuration. Falling back to console logging.");
                Console.WriteLine($"[SMS Dev Fallback] To: {phoneNumber}, Message: {message}");
                return (true, "SMS sent (dev fallback)");
            }

            TwilioClient.Init(accountSid, authToken);

            var messageOptions = new CreateMessageOptions(new PhoneNumber(phoneNumber))
            {
                From = new PhoneNumber(fromPhoneNumber),
                Body = message
            };

            var messageResource = await MessageResource.CreateAsync(messageOptions);
            _logger.LogInformation("SMS successfully sent to {PhoneNumber}. SID: {Sid}", phoneNumber, messageResource.Sid);

            return (true, "SMS sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {PhoneNumber}", phoneNumber);
            Console.WriteLine($"[SMS Dev Fallback on Error] To: {phoneNumber}, Message: {message}");
            return (false, "Failed to send SMS");
        }
    }
}
