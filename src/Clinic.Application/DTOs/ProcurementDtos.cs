using System;
using System.Collections.Generic;

namespace Clinic.Application.DTOs;

public class CreatePurchaseOrderDto
{
    public string ClinicId { get; set; } = string.Empty;
    public string SupplierId { get; set; } = string.Empty;
    public List<PurchaseOrderItemDto> Items { get; set; } = new();
}

public class PurchaseOrderItemDto
{
    public string MaterialId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
