using Clinic.Domain.Entities;
using Xunit;

namespace Clinic.UnitTests;

public class PrescriptionUnitTests
{
    [Fact]
    public void Prescription_MedicationCollection_ShouldRetainOrderedDrugs()
    {
        // Arrange
        var rx = new Prescription
        {
            Id = "rx-001",
            PatientId = "pat-001",
            DoctorId = "doc-001",
            Date = "2026-09-25",
            Notes = "Complete antibiotic course"
        };

        // Act
        rx.Medications.Add(new MedicationItem
        {
            Name = "Augmentin 1g",
            Dosage = "1 Tablet",
            Frequency = "Every 12 hours",
            Duration = "7 days"
        });
        rx.Medications.Add(new MedicationItem
        {
            Name = "Panadol Extra",
            Dosage = "2 Tablets",
            Frequency = "Every 8 hours as needed",
            Duration = "3 days"
        });

        // Assert
        Assert.Equal(2, rx.Medications.Count);
        Assert.Equal("Augmentin 1g", rx.Medications[0].Name);
        Assert.Equal("Panadol Extra", rx.Medications[1].Name);
    }
}
