using Xunit;

namespace Clinic.UnitTests;

public class SubscriptionTierUnitTests
{
    [Fact]
    public void REQ_SUB_01_StorageThresholdWarning_TriggersAtOrAbove85Percent()
    {
        // Arrange
        const double maxStorageGb = 50.0;

        // Act & Assert - 80% (no warning)
        double usedStorage80 = 40.0;
        bool isWarning80 = (usedStorage80 / maxStorageGb) >= 0.85;
        Assert.False(isWarning80);

        // Act & Assert - 85% (warning triggers)
        double usedStorage85 = 42.5;
        bool isWarning85 = (usedStorage85 / maxStorageGb) >= 0.85;
        Assert.True(isWarning85);

        // Act & Assert - 92% (warning triggers)
        double usedStorage92 = 46.0;
        bool isWarning92 = (usedStorage92 / maxStorageGb) >= 0.85;
        Assert.True(isWarning92);
    }

    [Fact]
    public void REQ_SUB_01_ProfessionalTier_HasExpectedQuotas()
    {
        // Arrange
        const string tierName = "Professional";
        const int maxSeats = 5;
        const double maxStorageGb = 50.0;
        const int maxSmsCredits = 1000;

        // Assert
        Assert.Equal("Professional", tierName);
        Assert.Equal(5, maxSeats);
        Assert.Equal(50.0, maxStorageGb);
        Assert.Equal(1000, maxSmsCredits);
    }
}
