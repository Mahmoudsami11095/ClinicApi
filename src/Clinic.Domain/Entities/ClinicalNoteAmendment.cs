namespace Clinic.Domain.Entities;

public class ClinicalNoteAmendment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string OriginalNoteId { get; set; } = string.Empty;
    public string AmendedText { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
}
