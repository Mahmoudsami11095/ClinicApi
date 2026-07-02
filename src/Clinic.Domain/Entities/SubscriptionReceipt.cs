using System;

namespace Clinic.Domain.Entities;

public class SubscriptionReceipt
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DoctorId { get; set; } = string.Empty;
    public string ReceiptUrl { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "PendingApproval"; // PendingApproval, Approved, Rejected
}
