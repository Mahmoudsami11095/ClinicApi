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
    private readonly string _openWaApiUrl;
    private readonly string _apiKey;
    private readonly string _sessionId;
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
        _openWaApiUrl = section["OpenWaApiUrl"] ?? "http://localhost:3000/api/sessions/{session-id}/messages/send-text";
        _apiKey = section["ApiKey"] ?? "YOUR_API_KEY";
        _sessionId = section["SessionId"] ?? "default";
        _rateLimitSeconds = int.TryParse(section["RateLimitSeconds"], out var rLimit) ? rLimit : 60;
        _otpExpiryMinutes = int.TryParse(section["OtpExpiryMinutes"], out var oExpiry) ? oExpiry : 5;
    }

    public async Task<(bool Success, string Message)> RequestOtpAsync(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return (false, "Phone number is required.");
        }

        var key = phoneNumber.Trim().ToLowerInvariant();

        // ── 1. Rate Limiting ──
        if (_rateLimitStore.TryGetValue(key, out var nextAllowedTime))
        {
            if (DateTime.UtcNow < nextAllowedTime)
            {
                var remaining = Math.Ceiling((nextAllowedTime - DateTime.UtcNow).TotalSeconds);
                return (false, $"Too many OTP requests. Please wait {remaining} more seconds before requesting a new code.");
            }
        }

        // ── 2. Generate 6-Digit OTP ──
        var code = Random.Shared.Next(100000, 999999).ToString();
        var expiryTime = DateTime.UtcNow.AddMinutes(_otpExpiryMinutes);

        // Store OTP
        _otpStore[key] = (code, expiryTime);

        // Update rate limiter timestamp
        _rateLimitStore[key] = DateTime.UtcNow.AddSeconds(_rateLimitSeconds);

        // ── 3. Send via OpenWA REST API ──
        try
        {
            var requestUrl = _openWaApiUrl.Replace("{session-id}", _sessionId);
            var formattedJid = phoneNumber.Contains("@") ? phoneNumber : $"{phoneNumber.TrimStart('+')}@s.whatsapp.net";

            var requestBody = new
            {
                jid = formattedJid,
                text = $"Your verification code is: {code}"
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = httpContent
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            _logger.LogInformation("Sending WhatsApp OTP to {PhoneNumber} via OpenWA API", phoneNumber);
            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("WhatsApp OTP successfully sent to {PhoneNumber}", phoneNumber);
                return (true, "OTP sent successfully.");
            }
            else
            {
                var responseError = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send WhatsApp OTP. Gateway status code: {StatusCode}, Error: {Error}", response.StatusCode, responseError);
                return (false, "Failed to deliver OTP message via WhatsApp API.");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP client error while sending WhatsApp OTP to {PhoneNumber}", phoneNumber);
            return (false, "WhatsApp API Gateway is currently unreachable.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while sending WhatsApp OTP to {PhoneNumber}", phoneNumber);
            return (false, "An unexpected error occurred while sending OTP.");
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
