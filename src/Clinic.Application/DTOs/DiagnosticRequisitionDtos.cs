namespace Clinic.Application.DTOs;

public class DiagnosticRequisitionDto
{
    public string Id { get; set; } = string.Empty;
    public string RequisitionToken { get; set; } = string.Empty;
    public string ClinicId { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string? PatientPhone { get; set; }
    public int? ToothNumber { get; set; }
    public string ServiceType { get; set; } = "DentalLab";
    public string Indications { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? PartnerName { get; set; }
    public List<string> ResultFileUrls { get; set; } = new();
    public string? PartnerNotes { get; set; }
    public string? TechnicianName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? FulfilledAt { get; set; }
}

public class CreateDiagnosticRequisitionDto
{
    public string ClinicId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public int? ToothNumber { get; set; }
    public string ServiceType { get; set; } = "DentalLab"; // "DentalLab", "Radiology", "Pathology"
    public string Indications { get; set; } = string.Empty;
    public string? PartnerName { get; set; }
}

public class PartnerUploadResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int FilesProcessed { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public List<string> FileUrls { get; set; } = new();
}
