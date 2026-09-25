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

    [Fact]
    public void Patient_NameProperty_ShouldCombineFirstNameAndLastName()
    {
        var patient = new Patient { FirstName = "John", LastName = "Doe" };
        Assert.Equal("John Doe", patient.Name);
    }

    [Fact]
    public void ClaimsPrincipalExtensions_ShouldExtractClaimsCorrectly()
    {
        var claims = new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "user-123"),
            new System.Security.Claims.Claim("doctorId", "doc-456"),
            new System.Security.Claims.Claim("clinicId", "clinic-789"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "doctor"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, "test@clinic.com")
        };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "TestAuth");
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);

        Assert.Equal("user-123", Clinic.Application.Common.ClaimsPrincipalExtensions.GetUserId(principal));
        Assert.Equal("doc-456", Clinic.Application.Common.ClaimsPrincipalExtensions.GetDoctorId(principal));
        Assert.Equal("clinic-789", Clinic.Application.Common.ClaimsPrincipalExtensions.GetClinicId(principal));
        Assert.Equal("doctor", Clinic.Application.Common.ClaimsPrincipalExtensions.GetUserRole(principal));
        Assert.Equal("test@clinic.com", Clinic.Application.Common.ClaimsPrincipalExtensions.GetEmail(principal));

        System.Security.Claims.ClaimsPrincipal? nullPrincipal = null;
        Assert.Null(Clinic.Application.Common.ClaimsPrincipalExtensions.GetUserId(nullPrincipal));
        Assert.Null(Clinic.Application.Common.ClaimsPrincipalExtensions.GetDoctorId(nullPrincipal));
    }
}
