using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Clinic.Application.Interfaces;
using Clinic.Application.Services;
using Clinic.Domain.Entities;
using Clinic.Domain.Enums;
using Moq;
using Xunit;

namespace Clinic.UnitTests;

public class MaterialUnitTests
{
    [Fact]
    public void Material_Consumption_ShouldAccuratelyDecrementStock()
    {
        // Arrange
        var material = new Material
        {
            Id = "mat-001",
            Name = "Local Anesthetic Cartridges",
            Quantity = 50,
            Unit = "Carpule",
            ClinicId = "cln-001",
            DoctorId = "doc-001"
        };

        // Act
        material.Quantity -= 5;

        // Assert
        Assert.Equal(45, material.Quantity);
    }

    [Fact]
    public void Material_Properties_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var material = new Material
        {
            Id = "mat-002",
            Name = "Composite Resin Shade A2",
            Quantity = 12,
            Unit = "Syringe",
            ClinicId = "cln-001",
            DoctorId = "doc-001"
        };

        // Assert
        Assert.Equal("mat-002", material.Id);
        Assert.Equal("Composite Resin Shade A2", material.Name);
        Assert.Equal(12, material.Quantity);
        Assert.Equal("Syringe", material.Unit);
    }

    [Fact]
    public async Task BR_INV_02_WhenQuantityFallsBelowThreshold_TriggersAlertToDoctorAndAssistant()
    {
        // Arrange
        var mockNotificationService = new Moq.Mock<Clinic.Application.Interfaces.INotificationService>();
        var mockUserRepo = new Moq.Mock<Clinic.Application.Interfaces.IUserRepository>();

        var doctorUser = new User
        {
            Id = "user-doc-1",
            DoctorId = "doc-1",
            Role = Clinic.Domain.Enums.UserRole.Doctor,
            ClinicId = "clinic-1"
        };
        var assistantUser = new User
        {
            Id = "user-asst-1",
            Role = Clinic.Domain.Enums.UserRole.Assistant,
            ClinicId = "clinic-1"
        };
        var unrelatedUser = new User
        {
            Id = "user-other-clinic",
            Role = Clinic.Domain.Enums.UserRole.Assistant,
            ClinicId = "clinic-99"
        };

        mockUserRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { doctorUser, assistantUser, unrelatedUser });

        var alertService = new Clinic.Application.Services.MaterialAlertService(
            mockNotificationService.Object,
            mockUserRepo.Object
        );

        var lowStockMaterial = new Material
        {
            Id = "mat-1",
            Name = "Surgical Masks",
            Quantity = 4, // Below threshold of 10
            MinStockAlert = 10,
            Unit = "Boxes",
            ClinicId = "clinic-1",
            DoctorId = "doc-1"
        };

        // Act
        await alertService.CheckAndTriggerLowStockAlertAsync(lowStockMaterial);

        // Assert - Both doctor and assistant in clinic-1 receive the alert
        mockNotificationService.Verify(n => n.CreateNotificationAsync(
            "user-doc-1",
            "Low Stock Alert",
            Moq.It.Is<string>(s => s.Contains("Surgical Masks") && s.Contains("4 Boxes remaining")),
            "Inventory"
        ), Moq.Times.Once);

        mockNotificationService.Verify(n => n.CreateNotificationAsync(
            "user-asst-1",
            "Low Stock Alert",
            Moq.It.Is<string>(s => s.Contains("Surgical Masks") && s.Contains("4 Boxes remaining")),
            "Inventory"
        ), Moq.Times.Once);

        // Unrelated clinic assistant does NOT receive notification
        mockNotificationService.Verify(n => n.CreateNotificationAsync(
            "user-other-clinic",
            Moq.It.IsAny<string>(),
            Moq.It.IsAny<string>(),
            Moq.It.IsAny<string>()
        ), Moq.Times.Never);
    }

    [Fact]
    public async Task BR_INV_02_WhenQuantityEqualsThreshold_TriggersAlert()
    {
        // Arrange
        var mockNotificationService = new Moq.Mock<Clinic.Application.Interfaces.INotificationService>();
        var mockUserRepo = new Moq.Mock<Clinic.Application.Interfaces.IUserRepository>();

        var doctorUser = new User
        {
            Id = "user-doc-1",
            DoctorId = "doc-1",
            Role = Clinic.Domain.Enums.UserRole.Doctor,
            ClinicId = "clinic-1"
        };

        mockUserRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { doctorUser });

        var alertService = new Clinic.Application.Services.MaterialAlertService(
            mockNotificationService.Object,
            mockUserRepo.Object
        );

        var exactThresholdMaterial = new Material
        {
            Id = "mat-2",
            Name = "Latex Gloves",
            Quantity = 5, // Exactly equal to threshold
            MinStockAlert = 5,
            Unit = "Boxes",
            ClinicId = "clinic-1",
            DoctorId = "doc-1"
        };

        // Act
        await alertService.CheckAndTriggerLowStockAlertAsync(exactThresholdMaterial);

        // Assert
        mockNotificationService.Verify(n => n.CreateNotificationAsync(
            "user-doc-1",
            "Low Stock Alert",
            Moq.It.Is<string>(s => s.Contains("Latex Gloves") && s.Contains("5 Boxes remaining")),
            "Inventory"
        ), Moq.Times.Once);
    }

    [Fact]
    public async Task BR_INV_02_WhenQuantityExceedsThreshold_DoesNotTriggerAlert()
    {
        // Arrange
        var mockNotificationService = new Moq.Mock<Clinic.Application.Interfaces.INotificationService>();
        var mockUserRepo = new Moq.Mock<Clinic.Application.Interfaces.IUserRepository>();

        var alertService = new Clinic.Application.Services.MaterialAlertService(
            mockNotificationService.Object,
            mockUserRepo.Object
        );

        var healthyStockMaterial = new Material
        {
            Id = "mat-3",
            Name = "Dental Needles",
            Quantity = 50, // Above threshold of 10
            MinStockAlert = 10,
            Unit = "Pieces",
            ClinicId = "clinic-1"
        };

        // Act
        await alertService.CheckAndTriggerLowStockAlertAsync(healthyStockMaterial);

        // Assert - No notifications dispatched
        mockNotificationService.Verify(n => n.CreateNotificationAsync(
            Moq.It.IsAny<string>(),
            Moq.It.IsAny<string>(),
            Moq.It.IsAny<string>(),
            Moq.It.IsAny<string>()
        ), Moq.Times.Never);
    }

    [Fact]
    public async Task BR_INV_02_WhenUserClinicsContainsClinic_AssistantReceivesAlert()
    {
        // Arrange
        var mockNotificationService = new Moq.Mock<Clinic.Application.Interfaces.INotificationService>();
        var mockUserRepo = new Moq.Mock<Clinic.Application.Interfaces.IUserRepository>();

        var multiClinicAssistant = new User
        {
            Id = "user-multi-asst",
            Role = Clinic.Domain.Enums.UserRole.Assistant,
            UserClinics = new List<UserClinic>
            {
                new UserClinic { ClinicId = "clinic-secondary", UserId = "user-multi-asst" }
            }
        };

        mockUserRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { multiClinicAssistant });

        var alertService = new Clinic.Application.Services.MaterialAlertService(
            mockNotificationService.Object,
            mockUserRepo.Object
        );

        var lowStockMaterial = new Material
        {
            Id = "mat-4",
            Name = "Sterilization Pouches",
            Quantity = 2,
            MinStockAlert = 15,
            Unit = "Packs",
            ClinicId = "clinic-secondary"
        };

        // Act
        await alertService.CheckAndTriggerLowStockAlertAsync(lowStockMaterial);

        // Assert
        mockNotificationService.Verify(n => n.CreateNotificationAsync(
            "user-multi-asst",
            "Low Stock Alert",
            Moq.It.IsAny<string>(),
            "Inventory"
        ), Moq.Times.Once);
    }

    [Fact]
    public void BR_INV_01_WhenExpirationDateInPast_IsExpiredIsTrue()
    {
        var expiredMaterial = new Material
        {
            Id = "mat-exp-1",
            Name = "Composite Primer",
            BatchNumber = "LOT-2024-09",
            ExpirationDate = DateTime.UtcNow.AddDays(-10),
            Quantity = 5
        };

        Assert.True(expiredMaterial.IsExpired);
    }

    [Fact]
    public void BR_INV_01_WhenExpirationDateInFuture_IsExpiredIsFalse()
    {
        var validMaterial = new Material
        {
            Id = "mat-val-1",
            Name = "Composite Primer",
            BatchNumber = "LOT-2027-01",
            ExpirationDate = DateTime.UtcNow.AddYears(1),
            Quantity = 10
        };

        Assert.False(validMaterial.IsExpired);
    }

    [Fact]
    public void BR_INV_01_WhenExpirationDateNull_IsExpiredIsFalse()
    {
        var nonPerishableMaterial = new Material
        {
            Id = "mat-np-1",
            Name = "Mouth Mirrors",
            ExpirationDate = null,
            Quantity = 20
        };

        Assert.False(nonPerishableMaterial.IsExpired);
    }

    [Fact]
    public void BR_INV_01_MaterialDto_ReflectsExpirationAndBatchProperties()
    {
        var dto = new Clinic.Application.DTOs.MaterialDto
        {
            Id = "dto-1",
            Name = "Anesthetic Gel",
            BatchNumber = "BATCH-882",
            ExpirationDate = DateTime.UtcNow.AddMonths(-1),
            Quantity = 3
        };

        Assert.Equal("BATCH-882", dto.BatchNumber);
        Assert.True(dto.IsExpired);
    }

    [Fact]
    public void DefaultMaterialsCatalog_ShouldReturn59CuratedMaterialsWithStockZeroAndDefaultFlag()
    {
        // Act
        var materials = Clinic.Domain.Helpers.DefaultMaterialsCatalog.GetDefaultMaterials("clinic-seed-1", "doc-seed-1");

        // Assert
        Assert.Equal(59, materials.Count);
        Assert.All(materials, m =>
        {
            Assert.Equal("clinic-seed-1", m.ClinicId);
            Assert.Equal("doc-seed-1", m.DoctorId);
            Assert.Equal(0, m.Quantity);
            Assert.True(m.IsDefault);
            Assert.False(string.IsNullOrWhiteSpace(m.Name));
            Assert.False(string.IsNullOrWhiteSpace(m.Category));
            Assert.False(string.IsNullOrWhiteSpace(m.Unit));
            Assert.Null(m.SupplierName);
            Assert.True(m.UnitCost.HasValue && m.UnitCost.Value > 0);
        });
    }

    [Fact]
    public async Task MaterialSeedingService_WhenClinicHasNoDefaults_SeedsAllMaterials()
    {
        // Arrange
        var mockRepo = new Mock<IMaterialRepository>();
        mockRepo.Setup(r => r.GetByDoctorAndClinicAsync("doc-1", "clinic-1"))
            .ReturnsAsync(new List<Material>());

        var service = new MaterialSeedingService(mockRepo.Object);

        // Act
        await service.SeedDefaultMaterialsAsync("clinic-1", "doc-1");

        // Assert
        mockRepo.Verify(r => r.AddRangeAsync(It.Is<IEnumerable<Material>>(list => list.Count() == 59)), Times.Once);
    }

    [Fact]
    public async Task MaterialSeedingService_WhenClinicAlreadyHasDefaultMaterials_DoesNotDuplicate()
    {
        // Arrange
        var mockRepo = new Mock<IMaterialRepository>();
        var existingSeededMaterial = new Material
        {
            Id = "existing-1",
            ClinicId = "clinic-1",
            DoctorId = "doc-1",
            Name = "Sili Kit BMS",
            IsDefault = true,
            Quantity = 0
        };
        mockRepo.Setup(r => r.GetByDoctorAndClinicAsync("doc-1", "clinic-1"))
            .ReturnsAsync(new List<Material> { existingSeededMaterial });

        var service = new MaterialSeedingService(mockRepo.Object);

        // Act
        await service.SeedDefaultMaterialsAsync("clinic-1", "doc-1");

        // Assert - AddRangeAsync should never be invoked to prevent duplicates
        mockRepo.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<Material>>()), Times.Never);
    }
}
