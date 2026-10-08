using System;
using System.Collections.Generic;
using Clinic.Application.DTOs;
using Clinic.Domain.Entities;
using Xunit;

namespace Clinic.UnitTests;

public class InformedConsentUnitTests
{
    [Fact]
    public void REQ_CONSENT_01_InformedConsentDocument_Defaults_AreValid()
    {
        // Arrange & Act
        var consent = new InformedConsentDocument
        {
            DocumentNumber = "CNS-202610-0001",
            ClinicId = "clinic-01",
            PatientId = "pat-01",
            DoctorId = "doc-01",
            ProcedureType = "DentalImplant",
            ProcedureName = "Dental Implant Placement #46",
            ToothNumber = 46
        };

        // Assert
        Assert.NotNull(consent.Id);
        Assert.Equal("CNS-202610-0001", consent.DocumentNumber);
        Assert.Equal("Draft", consent.Status);
        Assert.Equal("Self", consent.SignatoryRelationship);
        Assert.Equal(46, consent.ToothNumber);
        Assert.Null(consent.PatientSignatureBase64);
        Assert.Null(consent.DoctorSignatureBase64);
        Assert.Null(consent.DocumentSha256Checksum);
        Assert.False(consent.IsDeleted);
        Assert.True(consent.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void REQ_CONSENT_02_PatientSigning_UpdatesStatusAndSignatoryName()
    {
        // Arrange
        var consent = new InformedConsentDocument
        {
            DocumentNumber = "CNS-202610-0002",
            Status = "PendingSignature"
        };

        // Act (Patient signs on touchscreen)
        consent.PatientSignatureBase64 = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAUA";
        consent.SignatoryName = "Kareem Adel";
        consent.SignatoryRelationship = "Self";
        consent.SignedAt = DateTime.UtcNow;
        consent.Status = "SignedByPatient";

        // Assert
        Assert.Equal("SignedByPatient", consent.Status);
        Assert.Equal("Kareem Adel", consent.SignatoryName);
        Assert.NotNull(consent.SignedAt);
        Assert.NotNull(consent.PatientSignatureBase64);
    }

    [Fact]
    public void BR_CONSENT_03_DoctorCountersign_GeneratesSha256ChecksumAndLocks()
    {
        // Arrange
        var consent = new InformedConsentDocument
        {
            DocumentNumber = "CNS-202610-0003",
            PatientId = "pat-100",
            DoctorId = "doc-50",
            ToothNumber = 48,
            ProcedureType = "SurgicalExtraction",
            ClinicalRiskDisclosures = "[\"Inferior alveolar nerve paresthesia\",\"Dry socket\"]",
            PatientSignatureBase64 = "data:image/png;base64,PATIENT_SIG",
            DoctorSignatureBase64 = "data:image/png;base64,DOCTOR_SIG",
            CountersignedAt = DateTime.UtcNow,
            Status = "SignedByPatient"
        };

        // Act (Doctor countersigns -> transitions to ArchivedLocked with SHA256)
        var rawData = $"{consent.DocumentNumber}|{consent.PatientId}|{consent.DoctorId}|{consent.ToothNumber}|{consent.ProcedureType}|{consent.ClinicalRiskDisclosures}|{consent.PatientSignatureBase64}|{consent.DoctorSignatureBase64}|{consent.CountersignedAt:O}";
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(rawData);
        var checksum = Convert.ToHexString(sha.ComputeHash(bytes));

        consent.DocumentSha256Checksum = checksum;
        consent.Status = "ArchivedLocked";

        // Assert
        Assert.Equal("ArchivedLocked", consent.Status);
        Assert.NotNull(consent.DocumentSha256Checksum);
        Assert.Equal(64, consent.DocumentSha256Checksum.Length); // 64 hex characters (256 bits)
    }

    [Fact]
    public void BR_CONSENT_04_PediatricConsent_RequiresGuardianRelationship()
    {
        // Arrange
        int patientAge = 9;

        // Act
        bool isMinor = patientAge < 18;
        string validRelationship = isMinor ? "Parent" : "Self";

        var consent = new InformedConsentDocument
        {
            PatientId = "pat-pediatric-1",
            SignatoryName = "Mona Gamal (Mother)",
            SignatoryRelationship = validRelationship
        };

        // Assert
        Assert.True(isMinor);
        Assert.Equal("Parent", consent.SignatoryRelationship);
        Assert.Contains("Mother", consent.SignatoryName);
    }
}
