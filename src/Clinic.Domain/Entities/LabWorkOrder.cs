using System;

namespace Clinic.Domain.Entities;

public class LabWorkOrder
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string OrderToken { get; set; } = "LAB-" + Guid.NewGuid().ToString("N")[..6].ToUpper();
    public string ClinicId { get; set; } = "clinic-main";
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public int ToothNumber { get; set; } // FDI notation (e.g. 11, 21, 36, 46)
    public string RestorationType { get; set; } = "Full Anatomical Crown";
    public string Material { get; set; } = "Monolithic Multi-Layer Zirconia";
    public string Shade { get; set; } = "A2";
    public string? StumpShade { get; set; }
    public string? StlScanUrl { get; set; }
    public string ModelFileFormat { get; set; } = "STL";
    public int MeshPolyCount { get; set; } = 48250;
    public long ScanFileSize { get; set; } = 2457600;
    public string? ModelBoundingBoxMm { get; set; } = "42.5 x 31.8 x 18.2 mm";
    public string Status { get; set; } = "Dispatched"; // Dispatched, ReceivedByLab, Milling, QualityControl, Shipped, ReceivedByClinic
    public string LabPartnerName { get; set; } = "Apex Precision Dental Milling Lab";
    public string? LabTrackingNumber { get; set; }
    public string? LabTechnicianNotes { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
    public DateTime? DeliveryReceivedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool CanScheduleFittingAppointment(out string reason)
    {
        if (string.Equals(Status, "ReceivedByClinic", StringComparison.OrdinalIgnoreCase))
        {
            reason = $"Restoration (Tooth #{ToothNumber}) has been physically received and inspected by clinic. Fitting appointment can proceed.";
            return true;
        }

        reason = $"Clinical Governance BR-LAB-01 Violation: Restoration (Tooth #{ToothNumber}) is currently '{Status}'. Fitting cannot be booked until piece is 'ReceivedByClinic'.";
        return false;
    }
}
