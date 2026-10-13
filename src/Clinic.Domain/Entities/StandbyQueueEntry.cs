using System;
using System.Collections.Generic;

namespace Clinic.Domain.Entities;

public class StandbyQueueEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string ClinicId { get; set; } = string.Empty;
    public ClinicEntity Clinic { get; set; } = null!;

    public string PatientId { get; set; } = string.Empty;
    public Patient Patient { get; set; } = null!;

    public string DoctorId { get; set; } = string.Empty;
    public Doctor Doctor { get; set; } = null!;

    public string RequestedDate { get; set; } = string.Empty; // YYYY-MM-DD
    public string Status { get; set; } = "Waiting"; // Waiting, Notified, Booked, Expired
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? NotifiedAt { get; set; }
}
