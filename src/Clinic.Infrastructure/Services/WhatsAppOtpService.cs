using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Clinic.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Clinic.Infrastructure.Services;

public class WhatsAppOtpService : IWhatsAppOtpService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WhatsAppOtpService> _logger;

    // Configuration keys
    private readonly string _metaApiUrl;
    private readonly string _phoneNumberId;
    private readonly string _accessToken;
    private readonly string _templateName;
    private readonly int _rateLimitSeconds;
    private readonly int _otpExpiryMinutes;

    // Stores OTP: Key is phone number, Value is (Code, Expiry)
    private static readonly ConcurrentDictionary<string, (string Code, DateTime Expiry)> _otpStore = new();

    // Stores rate limit timestamp: Key is phone number, Value is NextAllowedRequestTime
    private static readonly ConcurrentDictionary<string, DateTime> _rateLimitStore = new();

    public WhatsAppOtpService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<WhatsAppOtpService> _logger)
    {
        _httpClient = httpClient;
        this._logger = _logger;

        var section = configuration.GetSection("WhatsAppOtp");
        _metaApiUrl = section["MetaApiUrl"] ?? "https://graph.facebook.com/v20.0/";
        _phoneNumberId = section["PhoneNumberId"] ?? "";
        _accessToken = section["AccessToken"] ?? "";
        _templateName = section["TemplateName"] ?? "verify_code_1";
        _rateLimitSeconds = int.TryParse(section["RateLimitSeconds"], out var rLimit) ? rLimit : 60;
        _otpExpiryMinutes = int.TryParse(section["OtpExpiryMinutes"], out var oExpiry) ? oExpiry : 5;
    }

    public async Task<(bool Success, string Message, string? Code)> RequestOtpAsync(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return (false, "Phone number is required.", null);
        }

        var key = phoneNumber.Trim().ToLowerInvariant();

        // ── 1. Rate Limiting ──
        if (_rateLimitStore.TryGetValue(key, out var nextAllowedTime))
        {
            if (DateTime.UtcNow < nextAllowedTime)
            {
                var remaining = Math.Ceiling((nextAllowedTime - DateTime.UtcNow).TotalSeconds);
                return (false, $"Too many OTP requests. Please wait {remaining} more seconds before requesting a new code.", null);
            }
        }

        // ── 2. Generate 6-Digit OTP ──
        var code = Random.Shared.Next(100000, 999999).ToString();
        var expiryTime = DateTime.UtcNow.AddMinutes(_otpExpiryMinutes);

        // Store OTP
        _otpStore[key] = (code, expiryTime);

        // Update rate limiter timestamp
        _rateLimitStore[key] = DateTime.UtcNow.AddSeconds(_rateLimitSeconds);

        // ── 3. Send via Meta Graph API ──
        try
        {
            var requestUrl = $"{_metaApiUrl.TrimEnd('/')}/{_phoneNumberId}/messages";
            // Strip any '+' since Meta API requires digits only
            var toPhoneNumber = phoneNumber.TrimStart('+');

            var requestBody = new
            {
                messaging_product = "whatsapp",
                to = toPhoneNumber,
                type = "template",
                template = new
                {
                    name = _templateName,
                    language = new { code = "en_US" },
                    components = new object[]
                    {
                        new
                        {
                            type = "body",
                            parameters = new[]
                            {
                                new { type = "text", text = "Clinic User" }, // {{1}} Name
                                new { type = "text", text = code }         // {{2}} OTP
                            }
                        }
                    }
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = httpContent
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            _logger.LogInformation("Sending WhatsApp OTP to {PhoneNumber} via Meta API", phoneNumber);
            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("WhatsApp OTP successfully sent to {PhoneNumber}", phoneNumber);
                return (true, "OTP sent successfully.", code);
            }
            else
            {
                var responseError = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to send WhatsApp OTP. Gateway status code: {StatusCode}, Error: {Error}. Falling back to console logging.", response.StatusCode, responseError);
                Console.WriteLine($"[WhatsApp OTP Dev Fallback] Generated Code for {phoneNumber}: {code}");
                return (true, $"[DEV ONLY] Gateway returned error {response.StatusCode}. Code printed to console: {code}", code);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "WhatsApp API Gateway is unreachable. Falling back to console logging.");
            Console.WriteLine($"[WhatsApp OTP Dev Fallback] Generated Code for {phoneNumber}: {code}");
            return (true, $"[DEV ONLY] Gateway unreachable. Code printed to console: {code}", code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while sending WhatsApp OTP to {PhoneNumber}. Falling back to console logging.", phoneNumber);
            Console.WriteLine($"[WhatsApp OTP Dev Fallback] Generated Code for {phoneNumber}: {code}");
            return (true, $"[DEV ONLY] Code printed to console: {code}", code);
        }
    }

    public bool VerifyOtp(string phoneNumber, string code)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var key = phoneNumber.Trim().ToLowerInvariant();

        if (_otpStore.TryGetValue(key, out var entry))
        {
            return entry.Code == code && entry.Expiry > DateTime.UtcNow;
        }

        return false;
    }

    public void RemoveOtp(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber)) return;
        var key = phoneNumber.Trim().ToLowerInvariant();
        _otpStore.TryRemove(key, out _);
    }
}
