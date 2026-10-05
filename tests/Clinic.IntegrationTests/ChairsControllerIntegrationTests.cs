using Clinic.API.Controllers;
using Clinic.API.Hubs;
using Clinic.Application.DTOs;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Clinic.IntegrationTests;

public class ChairsControllerIntegrationTests
{
    private ClinicDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ClinicDbContext(options);
    }

    [Fact]
    public async Task GetChairs_WhenNoneExist_ShouldSeedStandardOperatories()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockHubContext = new Mock<IHubContext<NotificationHub>>();
        var controller = new ChairsController(context, mockHubContext.Object);

        // Act
        var result = await controller.GetChairs("clinic-101");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var chairs = Assert.IsAssignableFrom<IEnumerable<ClinicChair>>(okResult.Value);
        Assert.Equal(4, chairs.Count());
        Assert.Contains(chairs, c => c.RoomNumber == "101" && c.Status == "available");
        Assert.Contains(chairs, c => c.RoomNumber == "104" && c.ChairName.Contains("Hygiene"));
    }

    [Fact]
    public async Task AssignPatient_ShouldTransitionToOccupiedAndSetOccupancyTimestamp()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockHubContext = new Mock<IHubContext<NotificationHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockGroup = new Mock<IClientProxy>();
        var mockAll = new Mock<IClientProxy>();
        mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockGroup.Object);
        mockClients.Setup(c => c.All).Returns(mockAll.Object);

        var chair = new ClinicChair
        {
            Id = "chair-1",
            ClinicId = "clinic-1",
            RoomNumber = "101",
            ChairName = "Operatory 1",
            Status = "available"
        };
        await context.ClinicChairs.AddAsync(chair);
        await context.SaveChangesAsync();

        var controller = new ChairsController(context, mockHubContext.Object);

        var assignDto = new AssignChairDto
        {
            PatientId = "pat-123",
            PatientName = "Ahmed Mansour",
            DoctorId = "doc-456",
            DoctorName = "Dr. Mahmoud Sami",
            ProcedureName = "Crown Preparation",
            Notes = "Prefers topical anesthetic before injection"
        };

        // Act
        var result = await controller.AssignPatient("chair-1", assignDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var updatedChair = Assert.IsType<ClinicChair>(okResult.Value);
        Assert.Equal("occupied", updatedChair.Status);
        Assert.Equal("Ahmed Mansour", updatedChair.CurrentPatientName);
        Assert.Equal("Crown Preparation", updatedChair.ProcedureName);
        Assert.NotNull(updatedChair.OccupancyStartedAt);
        Assert.Null(updatedChair.CleaningStartedAt);

        // Verify SignalR dispatch to all clients
        mockClients.Verify(c => c.All, Times.Once);
    }

    [Fact]
    public async Task ReleaseChair_ShouldTransitionToCleaningAndTriggerSterilizationTimer()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockHubContext = new Mock<IHubContext<NotificationHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockAll = new Mock<IClientProxy>();
        var mockGroup = new Mock<IClientProxy>();
        mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockGroup.Object);
        mockClients.Setup(c => c.All).Returns(mockAll.Object);

        var chair = new ClinicChair
        {
            Id = "chair-2",
            ClinicId = "clinic-1",
            RoomNumber = "102",
            ChairName = "Operatory 2",
            Status = "occupied",
            CurrentPatientName = "Sarah Connor",
            ProcedureName = "Root Canal",
            OccupancyStartedAt = DateTime.UtcNow.AddMinutes(-45)
        };
        await context.ClinicChairs.AddAsync(chair);
        await context.SaveChangesAsync();

        var controller = new ChairsController(context, mockHubContext.Object);

        // Act
        var result = await controller.ReleaseChair("chair-2");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var updatedChair = Assert.IsType<ClinicChair>(okResult.Value);
        Assert.Equal("cleaning", updatedChair.Status);
        Assert.NotNull(updatedChair.CleaningStartedAt);
        Assert.Null(updatedChair.OccupancyStartedAt);
        Assert.Null(updatedChair.CurrentPatientName);
        Assert.Null(updatedChair.ProcedureName);
    }

    [Fact]
    public async Task CompleteCleaning_ShouldTransitionBackToAvailableAndClearTimers()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockHubContext = new Mock<IHubContext<NotificationHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockAll = new Mock<IClientProxy>();
        var mockGroup = new Mock<IClientProxy>();
        mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockGroup.Object);
        mockClients.Setup(c => c.All).Returns(mockAll.Object);

        var chair = new ClinicChair
        {
            Id = "chair-3",
            ClinicId = "clinic-1",
            RoomNumber = "103",
            ChairName = "Operatory 3",
            Status = "cleaning",
            CleaningStartedAt = DateTime.UtcNow.AddMinutes(-15)
        };
        await context.ClinicChairs.AddAsync(chair);
        await context.SaveChangesAsync();

        var controller = new ChairsController(context, mockHubContext.Object);

        // Act
        var result = await controller.CompleteCleaning("chair-3");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var updatedChair = Assert.IsType<ClinicChair>(okResult.Value);
        Assert.Equal("available", updatedChair.Status);
        Assert.Null(updatedChair.CleaningStartedAt);
        Assert.Null(updatedChair.OccupancyStartedAt);
    }

    [Fact]
    public async Task UpdateStatus_WithInvalidStatus_ShouldReturnBadRequest()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockHubContext = new Mock<IHubContext<NotificationHub>>();
        var chair = new ClinicChair
        {
            Id = "chair-4",
            ClinicId = "clinic-1",
            ChairName = "Operatory 4",
            Status = "available"
        };
        await context.ClinicChairs.AddAsync(chair);
        await context.SaveChangesAsync();

        var controller = new ChairsController(context, mockHubContext.Object);

        // Act
        var result = await controller.UpdateStatus("chair-4", new UpdateChairStatusDto { Status = "invalid-status" });

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }
}
