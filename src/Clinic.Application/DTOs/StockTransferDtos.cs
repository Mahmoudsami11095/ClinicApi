using System;
using System.ComponentModel.DataAnnotations;

namespace Clinic.Application.DTOs;

public class CreateStockTransferRequestDto
{
    [Required]
    public string SourceClinicId { get; set; } = string.Empty;

    [Required]
    public string DestinationClinicId { get; set; } = string.Empty;

    [Required]
    public string MaterialId { get; set; } = string.Empty;

    [Range(1, 10000)]
    public int QuantityRequested { get; set; }

    public string Priority { get; set; } = "Normal"; // "Normal", "Urgent", "Emergency"
    public string? Notes { get; set; }
}

public class ApproveStockTransferDto
{
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Notes { get; set; }
}

public class DispatchStockTransferDto
{
    [Range(1, 10000)]
    public int QuantityDispatched { get; set; }

    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Notes { get; set; }
}

public class ReceiveStockTransferDto
{
    [Range(0, 10000)]
    public int QuantityReceived { get; set; }

    [Range(0, 10000)]
    public int QuantityDamaged { get; set; } = 0;

    public string? DamageReason { get; set; }
    public string? Notes { get; set; }
}

public class StockTransferRequisitionResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string RequisitionNumber { get; set; } = string.Empty;

    public string SourceClinicId { get; set; } = string.Empty;
    public string SourceClinicName { get; set; } = string.Empty;

    public string DestinationClinicId { get; set; } = string.Empty;
    public string DestinationClinicName { get; set; } = string.Empty;

    public string MaterialId { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;

    public int QuantityRequested { get; set; }
    public int? QuantityDispatched { get; set; }
    public int? QuantityReceived { get; set; }
    public int QuantityDamaged { get; set; }

    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }

    public string Priority { get; set; } = "Normal";
    public string Status { get; set; } = "Requested";

    public string RequestedByUserId { get; set; } = string.Empty;
    public string? DispatchedByUserId { get; set; }
    public string? ReceivedByUserId { get; set; }

    public DateTime RequestedAt { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }

    public string? Notes { get; set; }
    public string? DamageReason { get; set; }
}
