using System;

namespace Clinic.Application.DTOs;

public class ClinicChairDto
{
    public string Id { get; set; } = string.Empty;
    public string ClinicId { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public string ChairName { get; set; } = string.Empty;
    public string Status { get; set; } = "available"; // available, occupied, cleaning, maintenance
    public string? CurrentPatientId { get; set; }
    public string? CurrentPatientName { get; set; }
    public string? CurrentDoctorId { get; set; }
    public string? CurrentDoctorName { get; set; }
    public string? ProcedureName { get; set; }
    public DateTime? OccupancyStartedAt { get; set; }
    public DateTime? CleaningStartedAt { get; set; }
    public string? Notes { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpdateChairStatusDto
{
    public string Status { get; set; } = "available";
    public string? Notes { get; set; }
}

public class AssignChairDto
{
    public string? PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string? DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string ProcedureName { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
