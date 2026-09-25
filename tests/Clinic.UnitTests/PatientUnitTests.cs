using Clinic.Domain.Entities;
using Clinic.Domain.Helpers;
using Xunit;

namespace Clinic.UnitTests;

public class PatientUnitTests
{
    [Fact]
    public void Patient_DefaultValues_ShouldBeInitializedProperly()
    {
        // Arrange & Act
        var patient = new Patient();

        // Assert
        Assert.False(patient.IsDeleted);
        Assert.Equal("+20", patient.CountryCode);
        Assert.Empty(patient.FirstName);
        Assert.Empty(patient.LastName);
        Assert.NotNull(patient.Appointments);
        Assert.NotNull(patient.Prescriptions);
        Assert.NotNull(patient.DentalLogs);
        Assert.NotNull(patient.BillingRecords);
        Assert.NotNull(patient.RadiologyRecords);
    }

    [Theory]
    [InlineData("+20 1001234567", "+20", "1001234567")]
    [InlineData("+1 5551234567", "+1", "5551234567")]
    [InlineData("01001234567", "+20", "01001234567")]
    public void SplitContactNumber_ValidInputs_ShouldExtractCountryCodeAndPhone(
        string input, string expectedCountryCode, string expectedPhone)
    {
        // Act
        var result = PhoneHelper.SplitContactNumber(input);

        // Assert
        Assert.Equal(expectedCountryCode, result.CountryCode);
        Assert.Equal(expectedPhone, result.PhoneNumber);
    }

    [Fact]
    public void Patient_ContactNumberProperty_ShouldSetAndFormatProperly()
    {
        // Arrange
        var patient = new Patient();

        // Act
        patient.ContactNumber = "+20 1555102395";

        // Assert
        Assert.Equal("+20", patient.CountryCode);
        Assert.Equal("1555102395", patient.PhoneNumber);
        Assert.Equal("+20 1555102395", patient.ContactNumber);
    }

    [Fact]
    public void Patient_SoftDelete_ShouldToggleFlagWithoutErasingData()
    {
        // Arrange
        var patient = new Patient
        {
            Id = "pat-001",
            FirstName = "Ahmed",
            LastName = "Ali",
            Allergies = "Penicillin",
            IsDeleted = false
        };

        // Act
        patient.IsDeleted = true;

        // Assert
        Assert.True(patient.IsDeleted);
        Assert.Equal("Ahmed", patient.FirstName);
        Assert.Equal("Penicillin", patient.Allergies);
    }
}
