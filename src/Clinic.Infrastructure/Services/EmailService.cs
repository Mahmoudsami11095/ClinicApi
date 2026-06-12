using System.Net;
using System.Net.Mail;
using Clinic.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Clinic.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var smtpSection = _config.GetSection("Smtp");
        var server = smtpSection["Server"];
        var portStr = smtpSection["Port"];
        var senderEmail = smtpSection["SenderEmail"];
        var senderName = smtpSection["SenderName"] ?? "Clinic Support";
        var username = smtpSection["Username"];
        var password = smtpSection["Password"];
        var enableSslStr = smtpSection["EnableSsl"] ?? "true";

        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(senderEmail))
        {
            _logger.LogWarning("SMTP configuration is incomplete. Skipping sending email to {Email}. Email body: {Body}", toEmail, body);
            Console.WriteLine($"\n[EMAIL NOT SENT - SMTP NOT CONFIGURED]");
            Console.WriteLine($"To: {toEmail}\nSubject: {subject}\nBody: {body}\n");
            return;
        }

        int port = int.TryParse(portStr, out var p) ? p : 587;
        bool enableSsl = !bool.TryParse(enableSslStr, out var ssl) || ssl;

        try
        {
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            using var smtpClient = new SmtpClient(server, port)
            {
                EnableSsl = enableSsl,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(username ?? senderEmail, password)
            };

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Successfully sent email to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email} via SMTP server {Server}", toEmail, server);
            Console.WriteLine($"\n[EMAIL SENDING FAILED]: {ex.Message}");
            Console.WriteLine($"Fallback print for verification: OTP code email to {toEmail} - {subject}");
        }
    }
}
