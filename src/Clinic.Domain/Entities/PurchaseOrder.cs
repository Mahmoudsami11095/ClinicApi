using System;
using System.Collections.Generic;

namespace Clinic.Domain.Entities;

public class Supplier
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public int AverageLeadTimeDays { get; set; } = 3;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PurchaseOrder
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PONumber { get; set; } = string.Empty; // PO-YYYYMM-XXXX
    
    public string ClinicId { get; set; } = string.Empty;
    public ClinicEntity Clinic { get; set; } = null!;

    public string SupplierId { get; set; } = string.Empty;
    public Supplier Supplier { get; set; } = null!;

    public string Status { get; set; } = "Draft"; // Draft, Sent, Received, Settled, Cancelled
    public decimal TotalAmount { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DispatchedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }

    public List<PurchaseOrderItem> Items { get; set; } = new();
}

public class PurchaseOrderItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PurchaseOrderId { get; set; } = string.Empty;
    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    public string MaterialId { get; set; } = string.Empty;
    public Material Material { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal => Quantity * UnitPrice;
}
