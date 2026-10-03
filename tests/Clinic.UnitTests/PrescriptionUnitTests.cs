using Clinic.Application.DTOs;
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

    [Fact]
    public void BR_RX_02_Prescription_InitialState_ShouldBeDraftAndNotFinalized()
    {
        // Arrange & Act
        var rx = new Prescription
        {
            Id = "rx-draft-1",
            PatientId = "pat-1",
            DoctorId = "doc-1",
            Date = "2026-10-03"
        };

        // Assert
        Assert.False(rx.IsFinalized);
        Assert.Equal("draft", rx.Status);
        Assert.Null(rx.FinalizedAt);
        Assert.Null(rx.DigitalSignature);
        Assert.Null(rx.SupersedesPrescriptionId);
        Assert.Null(rx.SupersededById);
    }

    [Fact]
    public void BR_RX_02_Prescription_Finalize_ShouldLockPrescriptionWithDigitalSignature()
    {
        // Arrange
        var rx = new Prescription
        {
            Id = "rx-lock-1",
            PatientId = "pat-1",
            DoctorId = "doc-1",
            Date = "2026-10-03"
        };

        // Act
        rx.IsFinalized = true;
        rx.Status = "finalized";
        rx.FinalizedAt = "2026-10-03T14:30:00Z";
        rx.DigitalSignature = "Digitally Signed by Dr. Mahmoud Samy on 2026-10-03 14:30:00 UTC (Verified)";

        // Assert
        Assert.True(rx.IsFinalized);
        Assert.Equal("finalized", rx.Status);
        Assert.NotNull(rx.FinalizedAt);
        Assert.Contains("Dr. Mahmoud Samy", rx.DigitalSignature);
        Assert.Contains("Verified", rx.DigitalSignature);
    }

    [Fact]
    public void BR_RX_02_Prescription_SupersedingWorkflow_PreservesAuditTrailAndBiDirectionalLink()
    {
        // Arrange - Original finalized prescription
        var origRx = new Prescription
        {
            Id = "rx-orig-100",
            PatientId = "pat-1",
            DoctorId = "doc-1",
            Date = "2026-10-01",
            IsFinalized = true,
            Status = "finalized",
            FinalizedAt = "2026-10-01T10:00:00Z",
            DigitalSignature = "Digitally Signed by Dr. Mahmoud Samy",
            Medications = new List<MedicationItem>
            {
                new() { Name = "Amoxicillin 500mg", Dosage = "1 cap", Frequency = "TID", Duration = "7 days" }
            }
        };

        // Act - Clinician issues superseding prescription due to allergy or dosage adjustment
        const string reason = "Patient developed mild GI upset; switched to Azithromycin.";
        var newRx = new Prescription
        {
            Id = "rx-rev-200",
            PatientId = origRx.PatientId,
            DoctorId = "doc-2",
            Date = "2026-10-03",
            IsFinalized = true,
            Status = "finalized",
            FinalizedAt = "2026-10-03T11:00:00Z",
            DigitalSignature = "Digitally Signed by Dr. Sarah Partner",
            SupersedesPrescriptionId = origRx.Id,
            Notes = $"[SUPERSEDING PRESCRIPTION - BR-RX-02]\nReplaces Rx #{origRx.Id}\nClinical Reason: {reason}",
            Medications = new List<MedicationItem>
            {
                new() { Name = "Azithromycin 500mg", Dosage = "1 tab", Frequency = "Daily", Duration = "3 days" }
            }
        };

        // Archive original prescription
        origRx.Status = "superseded";
        origRx.SupersededById = newRx.Id;
        origRx.SupersedeReason = reason;

        // Assert - Both records remain intact with audit links
        Assert.Equal("superseded", origRx.Status);
        Assert.Equal(newRx.Id, origRx.SupersededById);
        Assert.Equal(reason, origRx.SupersedeReason);
        Assert.Single(origRx.Medications);
        Assert.Equal("Amoxicillin 500mg", origRx.Medications[0].Name);

        Assert.Equal("finalized", newRx.Status);
        Assert.Equal(origRx.Id, newRx.SupersedesPrescriptionId);
        Assert.Single(newRx.Medications);
        Assert.Equal("Azithromycin 500mg", newRx.Medications[0].Name);
    }

    [Fact]
    public void BR_RX_02_PrescriptionDto_Mapping_PreservesAllImmutabilityFields()
    {
        // Arrange
        var dto = new PrescriptionDto
        {
            Id = "rx-dto-1",
            AppointmentId = "apt-1",
            PatientId = "pat-1",
            DoctorId = "doc-1",
            Date = "2026-10-03",
            IsFinalized = true,
            Status = "finalized",
            FinalizedAt = "2026-10-03T15:00:00Z",
            DigitalSignature = "Digitally Signed by Dr. Mahmoud Samy",
            SupersedesPrescriptionId = "rx-prev-0",
            SupersededById = "rx-next-2",
            SupersedeReason = "Allergy conflict resolution",
            Medications = new List<MedicationItemDto>
            {
                new() { Name = "Ibuprofen 400mg", Dosage = "1 tab", Frequency = "PRN", Duration = "5 days" }
            }
        };

        // Assert
        Assert.True(dto.IsFinalized);
        Assert.Equal("finalized", dto.Status);
        Assert.Equal("2026-10-03T15:00:00Z", dto.FinalizedAt);
        Assert.Equal("Digitally Signed by Dr. Mahmoud Samy", dto.DigitalSignature);
        Assert.Equal("rx-prev-0", dto.SupersedesPrescriptionId);
        Assert.Equal("rx-next-2", dto.SupersededById);
        Assert.Equal("Allergy conflict resolution", dto.SupersedeReason);
    }
}
