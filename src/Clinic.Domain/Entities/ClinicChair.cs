using System;

namespace Clinic.Domain.Entities;

public class ClinicChair
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ClinicId { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public string ChairName { get; set; } = string.Empty;
    public string Status { get; set; } = "available"; // available (Ready), occupied (In Chair), cleaning (Sterilization), maintenance
    public string? CurrentPatientId { get; set; }
    public string? CurrentPatientName { get; set; }
    public string? CurrentDoctorId { get; set; }
    public string? CurrentDoctorName { get; set; }
    public string? ProcedureName { get; set; }
    public DateTime? OccupancyStartedAt { get; set; }
    public DateTime? CleaningStartedAt { get; set; }
    public string? Notes { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
