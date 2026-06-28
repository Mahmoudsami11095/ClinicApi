using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Clinic.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Clinic.Infrastructure.Services;

public class WhatsAppNotificationService : IWhatsAppNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WhatsAppNotificationService> _logger;

    private readonly string _metaApiUrl;
    private readonly string _phoneNumberId;
    private readonly string _accessToken;

    public WhatsAppNotificationService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<WhatsAppNotificationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var section = configuration.GetSection("WhatsAppOtp");
        _metaApiUrl = section["MetaApiUrl"] ?? "https://graph.facebook.com/v20.0/";
        _phoneNumberId = section["PhoneNumberId"] ?? "";
        _accessToken = section["AccessToken"] ?? "";
    }

    public async Task<bool> SendAppointmentConfirmationAsync(string phoneNumber, string patientName, string clinicName, string appointmentType, string date, string time)
    {
        var parameters = new[]
        {
            new { type = "text", text = patientName }, // {{1}} Patient Name
            new { type = "text", text = clinicName },  // {{2}} Clinic Name
            new { type = "text", text = appointmentType }, // {{3}} Appointment Type
            new { type = "text", text = date },        // {{4}} Date
            new { type = "text", text = time }         // {{5}} Time
        };

        return await SendTemplateMessageAsync(phoneNumber, "appointment", parameters);
    }

    public async Task<bool> SendAppointmentCancellationAsync(string phoneNumber, string patientName, string clinicName, string date, string time)
    {
        var parameters = new[]
        {
            new { type = "text", text = patientName }, // {{1}} Patient Name
            new { type = "text", text = clinicName },  // {{2}} Clinic Name
            new { type = "text", text = date },        // {{3}} Date
            new { type = "text", text = time }         // {{4}} Time
        };

        return await SendTemplateMessageAsync(phoneNumber, "appointment_cancellation", parameters);
    }

    private async Task<bool> SendTemplateMessageAsync(string phoneNumber, string templateName, object[] parameters)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            _logger.LogWarning("Cannot send WhatsApp template {TemplateName}. Phone number is empty.", templateName);
            return false;
        }

        try
        {
            var requestUrl = $"{_metaApiUrl.TrimEnd('/')}/{_phoneNumberId}/messages";
            var toPhoneNumber = phoneNumber.TrimStart('+'); // Meta API expects numbers without '+'

            var requestBody = new
            {
                messaging_product = "whatsapp",
                to = toPhoneNumber,
                type = "template",
                template = new
                {
                    name = templateName,
                    language = new { code = "en_US" },
                    components = new object[]
                    {
                        new
                        {
                            type = "body",
                            parameters = parameters
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

            _logger.LogInformation("Sending WhatsApp template {TemplateName} to {PhoneNumber}", templateName, phoneNumber);
            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully sent WhatsApp template {TemplateName} to {PhoneNumber}", templateName, phoneNumber);
                return true;
            }
            else
            {
                var responseError = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to send WhatsApp template {TemplateName}. Status: {StatusCode}, Error: {Error}", templateName, response.StatusCode, responseError);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while sending WhatsApp template {TemplateName} to {PhoneNumber}", templateName, phoneNumber);
            return false;
        }
    }
}
