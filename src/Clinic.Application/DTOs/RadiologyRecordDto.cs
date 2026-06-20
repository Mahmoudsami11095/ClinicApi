namespace Clinic.Application.DTOs;

public class RadiologyRecordDto
{
    public string Id { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string RadiologyCenterId { get; set; } = string.Empty;
    public string RadiologyCenterName { get; set; } = string.Empty;
    public string ProcedureName { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
    public string Date { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class CreateRadiologyRecordDto
{
    public string DoctorId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string RadiologyCenterId { get; set; } = string.Empty;
    public string ProcedureName { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
    public string Date { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
