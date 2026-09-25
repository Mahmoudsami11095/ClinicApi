using System.Text.Json;
using Clinic.Domain.Entities;
using Clinic.Domain.Enums;
using Xunit;

namespace Clinic.UnitTests;

public class DentalUnitTests
{
    [Fact]
    public void DentalLog_DefaultConsumedMaterials_ShouldBeEmptyJsonArray()
    {
        // Arrange & Act
        var log = new DentalLog();

        // Assert
        Assert.Equal("[]", log.ConsumedMaterials);
    }

    [Theory]
    [InlineData("16", false)] // Permanent molar
    [InlineData("21", false)] // Permanent central incisor
    [InlineData("A", true)]   // Deciduous primary tooth
    [InlineData("E", true)]   // Deciduous primary tooth
    public void DentalLog_ToothNotation_ShouldHandleAdultAndPediatricIdentifiers(string toothNumber, bool isPediatric)
    {
        // Arrange
        var log = new DentalLog
        {
            Id = "den-001",
            PatientId = "pat-001",
            ToothNumber = toothNumber,
            DoctorName = "Dr. Samy"
        };

        // Assert
        Assert.Equal(toothNumber, log.ToothNumber);
        if (isPediatric)
        {
            Assert.True(char.IsLetter(toothNumber[0]));
        }
        else
        {
            Assert.True(int.TryParse(toothNumber, out int num) && num >= 1 && num <= 32);
        }
    }

    [Fact]
    public void DentalLog_StatusJsonSerialization_ShouldSupportMultipleToothStatuses()
    {
        // Arrange
        var statuses = new List<string>
        {
            nameof(ToothStatus.Caries),
            nameof(ToothStatus.Filled)
        };
        var serialized = JsonSerializer.Serialize(statuses);

        var log = new DentalLog
        {
            Id = "den-002",
            Status = serialized,
            PainLevel = 5,
            Treatment = "Composite Restoration"
        };

        // Act
        var deserialized = JsonSerializer.Deserialize<List<string>>(log.Status);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Contains(nameof(ToothStatus.Caries), deserialized);
        Assert.Contains(nameof(ToothStatus.Filled), deserialized);
        Assert.Equal(5, log.PainLevel);
    }

    [Fact]
    public void DentalLog_PlanningToggle_ShouldDifferentiatePlannedVsCompleted()
    {
        // Arrange
        var plannedLog = new DentalLog { IsPlanned = true, Treatment = "Root Canal Therapy" };
        var completedLog = new DentalLog { IsPlanned = false, Treatment = "Root Canal Therapy (Obturation)" };

        // Assert
        Assert.True(plannedLog.IsPlanned);
        Assert.False(completedLog.IsPlanned);
    }
}
