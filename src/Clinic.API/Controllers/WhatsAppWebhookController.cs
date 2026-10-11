using System;
using System.Threading.Tasks;
using Clinic.Application.Interfaces;
using Clinic.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/webhooks/whatsapp")]
[AllowAnonymous]
public class WhatsAppWebhookController : ControllerBase
{
    private readonly ClinicDbContext _context;
    private readonly IWhatsAppNotificationService _whatsAppService;

    public WhatsAppWebhookController(ClinicDbContext context, IWhatsAppNotificationService whatsAppService)
    {
        _context = context;
        _whatsAppService = whatsAppService;
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveWebhook([FromBody] WhatsAppWebhookPayload payload)
    {
        if (payload?.Interactive?.ButtonReply != null)
        {
            var buttonId = payload.Interactive.ButtonReply.Id;
            var patientPhone = payload.From;

            if (buttonId.StartsWith("confirm_appt_"))
            {
                var apptId = buttonId.Replace("confirm_appt_", "");
                var appt = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == apptId);
                
                if (appt != null && appt.Status != "confirmed")
                {
                    appt.Status = "confirmed";
                    await _context.SaveChangesAsync();
                    
                    // Reply to patient
                    await _whatsAppService.SendNotificationAsync(patientPhone, "Thank you! Your appointment has been confirmed.");
                }
            }
            else if (buttonId.StartsWith("cancel_appt_"))
            {
                var apptId = buttonId.Replace("cancel_appt_", "");
                var appt = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == apptId);
                
                if (appt != null)
                {
                    appt.Status = "cancelled";
                    await _context.SaveChangesAsync();
                    
                    await _whatsAppService.SendNotificationAsync(patientPhone, "Your appointment has been cancelled. Let us know if you want to reschedule.");
                }
            }
        }
        else if (!string.IsNullOrEmpty(payload?.Text?.Body))
        {
            // Human escalation (BR-WA-03)
            // Just log or route to Assistant Hub (simulated by updating patient notes or sending a system alert)
            var patientPhone = payload.From;
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.PhoneNumber == patientPhone || p.PhoneNumber.EndsWith(patientPhone));
            if (patient != null)
            {
                patient.Notes = $"[WhatsApp Inbox {DateTime.UtcNow:O}]: {payload.Text.Body}\n{patient.Notes}";
                await _context.SaveChangesAsync();
            }
        }

        return Ok(new { success = true });
    }
}

public class WhatsAppWebhookPayload
{
    public string From { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public InteractivePayload? Interactive { get; set; }
    public TextPayload? Text { get; set; }
}

public class InteractivePayload
{
    public string Type { get; set; } = string.Empty;
    public ButtonReplyPayload? ButtonReply { get; set; }
}

public class ButtonReplyPayload
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}

public class TextPayload
{
    public string Body { get; set; } = string.Empty;
}
