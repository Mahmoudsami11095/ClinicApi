using Clinic.Domain.Entities;
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
}
