using System;
using System.Collections.Generic;
using Clinic.Application.DTOs;
using Clinic.Domain.Entities;
using Xunit;

namespace Clinic.UnitTests;

public class DiagnosticRequisitionAndPublicBookingUnitTests
{
    [Fact]
    public void REQ_PARTNER_01_DiagnosticRequisitionOrder_Defaults_AreValid()
    {
        // Arrange & Act
        var order = new DiagnosticRequisitionOrder
        {
            ClinicId = "clinic-01",
            DoctorId = "doc-01",
            PatientId = "pat-01",
            ToothNumber = 46,
            ServiceType = "DentalLab",
            Indications = "Zirconia Full Crown - Shade A2"
        };

        // Assert
        Assert.NotNull(order.Id);
        Assert.Equal("Pending", order.Status);
        Assert.Equal("[]", order.ResultFileUrls);
        Assert.False(order.IsDeleted);
        Assert.Equal(46, order.ToothNumber);
        Assert.Equal("DentalLab", order.ServiceType);
        Assert.True(order.CreatedAt <= DateTime.UtcNow);
        Assert.Null(order.FulfilledAt);
    }

    [Fact]
    public void REQ_PARTNER_02_DiagnosticRequisitionOrder_StatusTransitions_OperateCorrectly()
    {
        // Arrange
        var order = new DiagnosticRequisitionOrder
        {
            RequisitionToken = "ORD-A1B2C3",
            ClinicId = "clinic-01",
            DoctorId = "doc-01",
            PatientId = "pat-01",
            ServiceType = "Radiology"
        };

        // Act 1: Transition to InProgress
        order.Status = "InProgress";
        order.PartnerName = "Alfa Scan Diagnostic Center";
        Assert.Equal("InProgress", order.Status);

        // Act 2: Transition to ResultsReceived upon dropzone upload
        order.Status = "ResultsReceived";
        order.ResultFileUrls = "[\"uploads/patients/pat-01/diagnostics/cbct_molar.dcm\"]";
        order.TechnicianName = "Eng. Mohamed Taha";
        order.FulfilledAt = DateTime.UtcNow;

        // Assert
        Assert.Equal("ResultsReceived", order.Status);
        Assert.NotNull(order.FulfilledAt);
        Assert.Contains("cbct_molar.dcm", order.ResultFileUrls);
        Assert.Equal("Eng. Mohamed Taha", order.TechnicianName);
    }

    [Fact]
    public void REQ_QR_01_ClinicEntity_SupportsSlugAndPublicBookingAttributes()
    {
        // Arrange
        var clinic = new ClinicEntity
        {
            Id = "c-cairo-01",
            Name = "Al-Amal Dental Specialty Clinic",
            Slug = "al-amal-dental-cairo",
            PublicBookingEnabled = true,
            QrPosterAssetUrl = "https://cdn.clinic.com/posters/c-cairo-01.png"
        };

        // Assert
        Assert.Equal("al-amal-dental-cairo", clinic.Slug);
        Assert.True(clinic.PublicBookingEnabled);
        Assert.Equal("https://cdn.clinic.com/posters/c-cairo-01.png", clinic.QrPosterAssetUrl);
    }

    [Fact]
    public void REQ_QR_02_PublicClinicBookingMetadataDto_PopulatesCorrectly()
    {
        // Arrange
        var dto = new PublicClinicBookingMetadataDto
        {
            Id = "c-101",
            Name = "Smile Center",
            Slug = "smile-center",
            Address = "10 El-Bahr St, Tanta",
            Phone = "+201011223344",
            PublicBookingEnabled = true,
            Doctors = new List<PublicDoctorCardDto>
            {
                new PublicDoctorCardDto
                {
                    Id = "doc-505",
                    FullName = "Dr. Hazem Emam",
                    Specialization = "Orthodontics",
                    ClinicAvailabilityDays = new List<string> { "Sunday", "Tuesday", "Thursday" },
                    ClinicAvailabilityHours = "16:00-22:00"
                }
            }
        };

        // Assert
        Assert.Equal("smile-center", dto.Slug);
        Assert.Single(dto.Doctors);
        Assert.Equal("Dr. Hazem Emam", dto.Doctors[0].FullName);
        Assert.Equal(3, dto.Doctors[0].ClinicAvailabilityDays.Count);
    }

    [Fact]
    public void REQ_QR_01_ClinicQrKitDto_BuildsValidBookingAndQrUrls()
    {
        // Arrange
        var clinicId = "c-999";
        var slug = "downtown-family-dental";
        var bookingUrl = $"https://clinic-app-ten-topaz.vercel.app/book/{slug}";
        var expectedQrUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={Uri.EscapeDataString(bookingUrl)}";

        // Act
        var qrKit = new ClinicQrKitDto
        {
            ClinicId = clinicId,
            ClinicName = "Downtown Family Dental",
            Slug = slug,
            BookingUrl = bookingUrl,
            QrCodeDataUrl = expectedQrUrl
        };

        // Assert
        Assert.Equal("downtown-family-dental", qrKit.Slug);
        Assert.Contains(slug, qrKit.BookingUrl);
        Assert.Contains(Uri.EscapeDataString(bookingUrl), qrKit.QrCodeDataUrl);
    }
}
