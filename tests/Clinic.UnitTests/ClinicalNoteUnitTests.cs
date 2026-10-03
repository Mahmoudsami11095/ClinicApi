using Clinic.Application.DTOs;
using Clinic.Domain.Entities;
using Xunit;

namespace Clinic.UnitTests;

public class ClinicalNoteUnitTests
{
    [Fact]
    public void ClinicalNote_Creation_ShouldPreserveOriginalAuthorAndEmptyAmendments()
    {
        // Arrange & Act
        var note = new ClinicalNote
        {
            Id = "note-001",
            PatientId = "pat-101",
            DoctorId = "doc-001",
            DoctorName = "Dr. Mahmoud Samy",
            Title = "Initial Dental Evaluation",
            Category = "Consultation",
            Notes = "Patient presents with sensitivity on tooth 16 to cold liquids.",
            CreatedAt = "2026-10-03T10:00:00Z"
        };

        // Assert
        Assert.Equal("note-001", note.Id);
        Assert.Equal("pat-101", note.PatientId);
        Assert.Equal("doc-001", note.DoctorId);
        Assert.Equal("Dr. Mahmoud Samy", note.DoctorName);
        Assert.Equal("Patient presents with sensitivity on tooth 16 to cold liquids.", note.Notes);
        Assert.Empty(note.Amendments);
    }

    [Fact]
    public void BR_RX_03_Immutability_OriginalNotesNeverOverwritten_WhenAmendmentAppended()
    {
        // Arrange
        const string originalNoteText = "Patient complains of mild percussion tenderness in tooth 21.";
        var note = new ClinicalNote
        {
            Id = "note-imm-001",
            PatientId = "pat-101",
            DoctorId = "doc-001",
            DoctorName = "Dr. Mahmoud Samy",
            Notes = originalNoteText,
            CreatedAt = "2026-10-03T09:00:00Z"
        };

        // Act - Append an amendment
        const string amendedText = "Follow-up test: Tooth 21 tested negative for vitality on cold test. Plan root canal treatment.";
        var amendment = new ClinicalNoteAmendment
        {
            Id = "amend-001",
            OriginalNoteId = note.Id,
            AmendedText = amendedText,
            Reason = "Pulp vitality diagnostic result obtained post-initial consult.",
            AuthorId = "doc-002",
            AuthorName = "Dr. Sarah Specialist",
            Timestamp = "2026-10-03T11:30:00Z"
        };
        note.Amendments.Add(amendment);

        // Assert - BR-RX-03 / BR-MED-01: Original text is completely preserved
        Assert.Equal(originalNoteText, note.Notes);
        Assert.Single(note.Amendments);
        Assert.Equal("amend-001", note.Amendments[0].Id);
        Assert.Equal(note.Id, note.Amendments[0].OriginalNoteId);
        Assert.Equal(amendedText, note.Amendments[0].AmendedText);
        Assert.Equal("doc-002", note.Amendments[0].AuthorId);
        Assert.Equal("Dr. Sarah Specialist", note.Amendments[0].AuthorName);
        Assert.Equal("2026-10-03T11:30:00Z", note.Amendments[0].Timestamp);
    }

    [Fact]
    public void BR_RX_03_MultipleAmendments_MaintainsChronologicalAuditTrail_AndPreservesAuthors()
    {
        // Arrange
        var note = new ClinicalNote
        {
            Id = "note-multi-001",
            PatientId = "pat-101",
            DoctorId = "doc-001",
            DoctorName = "Dr. Mahmoud Samy",
            Notes = "Tooth 36 deep carious lesion evaluated.",
            CreatedAt = "2026-10-01T08:00:00Z"
        };

        // Act - Add Amendment 1
        note.Amendments.Add(new ClinicalNoteAmendment
        {
            Id = "amend-1",
            OriginalNoteId = note.Id,
            AmendedText = "Amendment 1: Temporary filling placed with IRM.",
            AuthorId = "doc-001",
            AuthorName = "Dr. Mahmoud Samy",
            Timestamp = "2026-10-01T09:00:00Z"
        });

        // Act - Add Amendment 2
        note.Amendments.Add(new ClinicalNoteAmendment
        {
            Id = "amend-2",
            OriginalNoteId = note.Id,
            AmendedText = "Amendment 2: Patient reports complete relief of pain. Advised crown preparation in 2 weeks.",
            Reason = "Patient check-in telephone call",
            AuthorId = "doc-003",
            AuthorName = "Dr. Kareem Partner",
            Timestamp = "2026-10-02T14:00:00Z"
        });

        // Assert
        Assert.Equal(2, note.Amendments.Count);
        Assert.Equal("Tooth 36 deep carious lesion evaluated.", note.Notes);
        Assert.Equal("doc-001", note.Amendments[0].AuthorId);
        Assert.Equal("doc-003", note.Amendments[1].AuthorId);
        Assert.Equal("Dr. Kareem Partner", note.Amendments[1].AuthorName);
        Assert.Equal("Patient check-in telephone call", note.Amendments[1].Reason);
    }

    [Fact]
    public void ClinicalNoteDto_Mapping_PreservesAllFieldsAndAmendments()
    {
        // Arrange
        var note = new ClinicalNote
        {
            Id = "note-dto-01",
            PatientId = "pat-101",
            DoctorId = "doc-001",
            DoctorName = "Dr. Mahmoud Samy",
            ClinicId = "clinic-01",
            Title = "Follow-up",
            Category = "Consultation",
            Notes = "Healing is progressing well.",
            CreatedAt = "2026-10-03T12:00:00Z",
            Amendments = new List<ClinicalNoteAmendment>
            {
                new()
                {
                    Id = "amend-dto-01",
                    OriginalNoteId = "note-dto-01",
                    AmendedText = "Sutures removed without complications.",
                    Reason = "Suture removal visit",
                    AuthorId = "doc-001",
                    AuthorName = "Dr. Mahmoud Samy",
                    Timestamp = "2026-10-03T12:30:00Z"
                }
            }
        };

        // Act - Simulate mapping to DTO
        var dto = new ClinicalNoteDto
        {
            Id = note.Id,
            PatientId = note.PatientId,
            DoctorId = note.DoctorId,
            DoctorName = note.DoctorName,
            ClinicId = note.ClinicId,
            Title = note.Title,
            Category = note.Category,
            Notes = note.Notes,
            CreatedAt = note.CreatedAt,
            Amendments = note.Amendments.Select(a => new ClinicalNoteAmendmentDto
            {
                Id = a.Id,
                OriginalNoteId = a.OriginalNoteId,
                AmendedText = a.AmendedText,
                Reason = a.Reason,
                AuthorId = a.AuthorId,
                AuthorName = a.AuthorName,
                Timestamp = a.Timestamp
            }).ToList()
        };

        // Assert
        Assert.Equal(note.Id, dto.Id);
        Assert.Equal(note.Notes, dto.Notes);
        Assert.Single(dto.Amendments);
        Assert.Equal("amend-dto-01", dto.Amendments[0].Id);
        Assert.Equal("Sutures removed without complications.", dto.Amendments[0].AmendedText);
    }
}
