using System;

namespace Clinic.Domain.Entities;

public class StockTransferRequisition
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string RequisitionNumber { get; set; } = string.Empty; // e.g. TRF-202610-0012

    public string SourceClinicId { get; set; } = string.Empty;
    public ClinicEntity SourceClinic { get; set; } = null!;

    public string DestinationClinicId { get; set; } = string.Empty;
    public ClinicEntity DestinationClinic { get; set; } = null!;

    public string MaterialId { get; set; } = string.Empty;
    public Material Material { get; set; } = null!;

    public int QuantityRequested { get; set; }
    public int? QuantityDispatched { get; set; }
    public int? QuantityReceived { get; set; }
    public int QuantityDamaged { get; set; } = 0;

    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }

    public string Priority { get; set; } = "Normal"; // "Normal", "Urgent", "Emergency"
    public string Status { get; set; } = "Requested"; // "Requested", "Approved", "InTransit", "Received", "Cancelled"

    public string RequestedByUserId { get; set; } = string.Empty;
    public string? DispatchedByUserId { get; set; }
    public string? ReceivedByUserId { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DispatchedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }

    public string? Notes { get; set; }
    public string? DamageReason { get; set; }

    public bool IsDeleted { get; set; } = false;
}
