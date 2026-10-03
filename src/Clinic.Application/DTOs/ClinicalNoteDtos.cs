namespace Clinic.Application.DTOs;

public class ClinicalNoteAmendmentDto
{
    public string Id { get; set; } = string.Empty;
    public string OriginalNoteId { get; set; } = string.Empty;
    public string AmendedText { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
}

public class ClinicalNoteDto
{
    public string Id { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string? ClinicId { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public List<ClinicalNoteAmendmentDto> Amendments { get; set; } = new();
}

public class CreateClinicalNoteDto
{
    public string PatientId { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Category { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string? ClinicId { get; set; }
}

public class AmendClinicalNoteDto
{
    public string AmendedText { get; set; } = string.Empty;
    public string? Reason { get; set; }
}
