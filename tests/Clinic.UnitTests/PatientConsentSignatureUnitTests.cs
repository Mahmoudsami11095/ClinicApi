using Clinic.Application.DTOs;
using Clinic.Domain.Entities;
using Xunit;

namespace Clinic.UnitTests;

public class PatientConsentSignatureUnitTests
{
    [Fact]
    public void REQ_PAT_03_Patient_StoresConsentSignatureAndTimestamp()
    {
        // Arrange
        var patient = new Patient
        {
            Id = "pat-sig-1",
            FirstName = "Yasmine",
            LastName = "Kamel",
            ClinicId = "clinic-1"
        };

        const string signatureData = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";
        const string timestamp = "2026-10-04T12:00:00Z";

        // Act
        patient.ConsentSignature = signatureData;
        patient.ConsentSignedAt = timestamp;

        // Assert
        Assert.Equal(signatureData, patient.ConsentSignature);
        Assert.Equal(timestamp, patient.ConsentSignedAt);
    }

    [Fact]
    public void REQ_PAT_03_PatientDto_PreservesConsentSignatureFields()
    {
        // Arrange
        const string signatureData = "data:image/png;base64,sample_signature_png";
        const string signedAt = "2026-10-04T12:30:00Z";

        var dto = new PatientDto
        {
            Id = "pat-dto-1",
            FirstName = "Yasmine",
            LastName = "Kamel",
            ConsentSignature = signatureData,
            ConsentSignedAt = signedAt
        };

        // Assert
        Assert.Equal(signatureData, dto.ConsentSignature);
        Assert.Equal(signedAt, dto.ConsentSignedAt);
    }
}
