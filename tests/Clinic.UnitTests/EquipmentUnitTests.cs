using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Moq;
using Xunit;

namespace Clinic.UnitTests;

public class EquipmentUnitTests
{
    [Fact]
    public void Equipment_Initialization_ShouldSetDefaultOperationalStatus()
    {
        // Arrange & Act
        var equipment = new Equipment
        {
            Id = "eq-001",
            Name = "Midmark M11 UltraClave",
            Category = "Sterilization",
            SerialNumber = "SN-M11-998",
            ClinicId = "clinic-101"
        };

        // Assert
        Assert.Equal("eq-001", equipment.Id);
        Assert.Equal("Midmark M11 UltraClave", equipment.Name);
        Assert.Equal("Sterilization", equipment.Category);
        Assert.Equal("SN-M11-998", equipment.SerialNumber);
        Assert.Equal("Operational", equipment.Status);
    }

    [Fact]
    public void Equipment_StatusTransitions_ShouldSupportAllLifecycleStates()
    {
        // Arrange
        var equipment = new Equipment
        {
            Id = "eq-002",
            Name = "A-dec 500 Dental Chair",
            Status = "Operational"
        };

        // Act & Assert
        equipment.Status = "Maintenance Due";
        Assert.Equal("Maintenance Due", equipment.Status);

        equipment.Status = "In Repair";
        Assert.Equal("In Repair", equipment.Status);

        equipment.Status = "Decommissioned";
        Assert.Equal("Decommissioned", equipment.Status);

        equipment.Status = "Operational";
        Assert.Equal("Operational", equipment.Status);
    }

    [Fact]
    public void Equipment_MaintenanceNotes_ShouldAppendLogProperly()
    {
        // Arrange
        var equipment = new Equipment
        {
            Id = "eq-003",
            Name = "Cavitron Ultrasonic Scaler",
            MaintenanceNotes = "2026-01-10: Initial installation and test."
        };

        // Act
        var newEntry = "2026-06-15: Scaler tip replacement and water line flush.";
        equipment.MaintenanceNotes = string.IsNullOrWhiteSpace(equipment.MaintenanceNotes)
            ? newEntry
            : $"{newEntry}\n---\n{equipment.MaintenanceNotes}";

        // Assert
        Assert.Contains("Scaler tip replacement", equipment.MaintenanceNotes);
        Assert.Contains("Initial installation", equipment.MaintenanceNotes);
        Assert.StartsWith("2026-06-15", equipment.MaintenanceNotes);
    }

    [Fact]
    public void Equipment_IsMaintenanceDue_EvaluatesPastAndFutureDatesAccurately()
    {
        // Arrange
        var pastDueEquipment = new Equipment
        {
            Id = "eq-overdue",
            Name = "Autoclave Chamber",
            NextMaintenanceDate = DateTime.UtcNow.AddDays(-2),
            Status = "Operational"
        };

        var futureEquipment = new Equipment
        {
            Id = "eq-good",
            Name = "Curing Light",
            NextMaintenanceDate = DateTime.UtcNow.AddDays(45),
            Status = "Operational"
        };

        var noDateEquipment = new Equipment
        {
            Id = "eq-none",
            Name = "Handpiece",
            NextMaintenanceDate = null
        };

        // Assert
        Assert.True(pastDueEquipment.IsMaintenanceDue);
        Assert.False(futureEquipment.IsMaintenanceDue);
        Assert.False(noDateEquipment.IsMaintenanceDue);
    }

    [Fact]
    public void Equipment_IsWarrantyExpired_EvaluatesAccurately()
    {
        // Arrange
        var expiredWarranty = new Equipment
        {
            WarrantyExpiryDate = DateTime.UtcNow.AddDays(-30)
        };

        var activeWarranty = new Equipment
        {
            WarrantyExpiryDate = DateTime.UtcNow.AddDays(180)
        };

        // Assert
        Assert.True(expiredWarranty.IsWarrantyExpired);
        Assert.False(activeWarranty.IsWarrantyExpired);
    }

    [Fact]
    public void EquipmentDto_Mapping_ShouldTransferAllPropertiesAccurately()
    {
        // Arrange
        var equipment = new Equipment
        {
            Id = "eq-map-1",
            ClinicId = "clinic-alpha",
            DoctorId = "doc-1",
            Name = "Woodpecker LED.B",
            Category = "Curing & Lights",
            ModelNumber = "LED.B-Pro",
            SerialNumber = "SN-WPK-2026",
            Manufacturer = "Woodpecker",
            RoomOrChair = "Operatory 2",
            PurchaseCost = 450.00m,
            PurchaseDate = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            WarrantyExpiryDate = new DateTime(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            Status = "Operational",
            LastMaintenanceDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            NextMaintenanceDate = DateTime.UtcNow.AddDays(90),
            ServiceProvider = "Global Dental Tech",
            ServiceContactPhone = "+201000000000",
            MaintenanceNotes = "Calibrated radiometer 1200 mW/cm2",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var dto = new EquipmentDto
        {
            Id = equipment.Id,
            ClinicId = equipment.ClinicId,
            DoctorId = equipment.DoctorId,
            Name = equipment.Name,
            Category = equipment.Category,
            ModelNumber = equipment.ModelNumber,
            SerialNumber = equipment.SerialNumber,
            Manufacturer = equipment.Manufacturer,
            RoomOrChair = equipment.RoomOrChair,
            PurchaseCost = equipment.PurchaseCost,
            PurchaseDate = equipment.PurchaseDate,
            WarrantyExpiryDate = equipment.WarrantyExpiryDate,
            Status = equipment.Status,
            LastMaintenanceDate = equipment.LastMaintenanceDate,
            NextMaintenanceDate = equipment.NextMaintenanceDate,
            ServiceProvider = equipment.ServiceProvider,
            ServiceContactPhone = equipment.ServiceContactPhone,
            MaintenanceNotes = equipment.MaintenanceNotes,
            CreatedAt = equipment.CreatedAt,
            IsMaintenanceDue = equipment.IsMaintenanceDue,
            IsWarrantyExpired = equipment.IsWarrantyExpired
        };

        // Assert
        Assert.Equal("eq-map-1", dto.Id);
        Assert.Equal("clinic-alpha", dto.ClinicId);
        Assert.Equal("Woodpecker LED.B", dto.Name);
        Assert.Equal("Curing & Lights", dto.Category);
        Assert.Equal("LED.B-Pro", dto.ModelNumber);
        Assert.Equal("SN-WPK-2026", dto.SerialNumber);
        Assert.Equal("Operatory 2", dto.RoomOrChair);
        Assert.Equal(450.00m, dto.PurchaseCost);
        Assert.Equal("Global Dental Tech", dto.ServiceProvider);
        Assert.False(dto.IsMaintenanceDue);
        Assert.False(dto.IsWarrantyExpired);
    }

    [Fact]
    public async Task EquipmentRepository_MockInteraction_SupportsGetByClinicAndAdd()
    {
        // Arrange
        var mockRepo = new Mock<IEquipmentRepository>();
        var items = new List<Equipment>
        {
            new() { Id = "eq-r1", Name = "Dental Unit 1", ClinicId = "cl-1" },
            new() { Id = "eq-r2", Name = "Dental Unit 2", ClinicId = "cl-1" }
        };

        mockRepo.Setup(r => r.GetByClinicIdAsync("cl-1")).ReturnsAsync(items);
        mockRepo.Setup(r => r.AddAsync(It.IsAny<Equipment>())).Returns(Task.CompletedTask);

        // Act
        var result = await mockRepo.Object.GetByClinicIdAsync("cl-1");
        await mockRepo.Object.AddAsync(new Equipment { Id = "eq-r3", Name = "Autoclave", ClinicId = "cl-1" });

        // Assert
        Assert.Equal(2, result.Count());
        mockRepo.Verify(r => r.GetByClinicIdAsync("cl-1"), Times.Once);
        mockRepo.Verify(r => r.AddAsync(It.IsAny<Equipment>()), Times.Once);
    }
}
