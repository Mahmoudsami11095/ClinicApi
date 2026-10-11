using System.Threading.Tasks;
using Clinic.API.Controllers;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Clinic.UnitTests.Controllers;

public class WhatsAppWebhookTests
{
    private ClinicDbContext GetMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
            .Options;
        return new ClinicDbContext(options);
    }

    [Fact]
    public async Task REQ_WA_01_Webhook_Confirms_Appointment()
    {
        // Arrange
        var context = GetMemoryContext();
        var appt = new Appointment { Id = "appt-100", Status = "scheduled" };
        context.Appointments.Add(appt);
        await context.SaveChangesAsync();

        var waMock = new Mock<IWhatsAppNotificationService>();
        var controller = new WhatsAppWebhookController(context, waMock.Object);

        var payload = new WhatsAppWebhookPayload
        {
            From = "+2010123",
            Type = "interactive",
            Interactive = new InteractivePayload
            {
                Type = "button_reply",
                ButtonReply = new ButtonReplyPayload { Id = "confirm_appt_appt-100" }
            }
        };

        // Act
        var result = await controller.ReceiveWebhook(payload);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var dbAppt = await context.Appointments.FindAsync("appt-100");
        Assert.Equal("confirmed", dbAppt!.Status);
        waMock.Verify(w => w.SendNotificationAsync("+2010123", It.IsAny<string>()), Times.Once);
    }
}
