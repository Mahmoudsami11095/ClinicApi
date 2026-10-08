namespace Clinic.Domain.Entities;

public class DiagnosticRequisitionOrder
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string RequisitionToken { get; set; } = string.Empty; // Unique short token e.g. "ORD-8F29A"

    public string ClinicId { get; set; } = string.Empty;
    public ClinicEntity Clinic { get; set; } = null!;

    public string DoctorId { get; set; } = string.Empty;
    public Doctor Doctor { get; set; } = null!;

    public string PatientId { get; set; } = string.Empty;
    public Patient Patient { get; set; } = null!;

    public int? ToothNumber { get; set; } // Optional FDI tooth number (e.g. 16, 46)
    public string ServiceType { get; set; } = "DentalLab"; // "DentalLab", "Radiology", "Pathology"
    public string Indications { get; set; } = string.Empty; // e.g. "Zirconia Full Crown - Shade A2"
    public string Status { get; set; } = "Pending"; // "Pending", "InProgress", "ResultsReceived", "Completed", "Cancelled"
    public string? PartnerName { get; set; } // e.g. "Apex Dental Lab", "Alfa Scan"
    
    public string ResultFileUrls { get; set; } = "[]"; // JSON array of uploaded file paths
    public string? PartnerNotes { get; set; }
    public string? TechnicianName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FulfilledAt { get; set; }

    public bool IsDeleted { get; set; } = false;
}
