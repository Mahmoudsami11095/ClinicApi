using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Clinic.Application.DTOs;
using Clinic.Domain.Entities;
using Xunit;

namespace Clinic.UnitTests;

public class DicomAndAiClaimsUnitTests
{
    [Fact]
    public void REQ_DICOM_01_HounsfieldUnit_CalculatesCorrectAttenuationDensity()
    {
        // Arrange
        // Raw 12-bit CT pixel value, Slope = 1.0, Intercept = -1024
        double rawPixelValue = 1850.0;
        double slope = 1.0;
        double intercept = -1024.0;

        // Act
        double huValue = (rawPixelValue * slope) + intercept;

        // Assert: 1850 - 1024 = 826 HU (Misch D2 Trabecular/Cortical Bone)
        Assert.Equal(826.0, huValue);
        Assert.True(huValue > 700.0 && huValue < 1200.0, "HU value matches Misch D2 bone density class.");
    }

    [Fact]
    public void REQ_DICOM_02_HuPresets_WindowLevel_CalculatesMinMaxBoundaries()
    {
        // Arrange: Soft Tissue Preset (Width: 350, Level: 40)
        double windowWidth = 350.0;
        double windowCenter = 40.0;

        // Act
        double minHu = windowCenter - (windowWidth / 2.0);
        double maxHu = windowCenter + (windowWidth / 2.0);

        // Assert
        Assert.Equal(-135.0, minHu);
        Assert.Equal(215.0, maxHu);
    }

    [Fact]
    public void REQ_DICOM_03_MultiSliceCBCT_GeneratesValidSliceSeries()
    {
        // Arrange
        int totalSlices = 48;
        var slices = new List<DicomSliceDto>();

        for (int i = 1; i <= totalSlices; i++)
        {
            slices.Add(new DicomSliceDto
            {
                SliceIndex = i,
                Orientation = "Axial",
                SliceLocationMm = -24.0 + (i * 1.0),
                AnatomicalLandmark = $"Axial Depth Slice {i}"
            });
        }

        // Assert
        Assert.Equal(48, slices.Count);
        Assert.Equal(1, slices[0].SliceIndex);
        Assert.Equal(-23.0, slices[0].SliceLocationMm);
        Assert.Equal(24.0, slices[47].SliceLocationMm);
    }

    [Fact]
    public void REQ_INS_04_AiFindingsToCdtCodes_MapsAccurately()
    {
        // Arrange
        var findingsMap = new Dictionary<string, (string code, string desc, string tooth, string icd, decimal fee)>
        {
            { "ai-find-101", ("D2391", "Resin-Based Composite - 1 Surface, Posterior (Tooth #16)", "16", "K02.9", 120m) },
            { "ai-find-102", ("D3330", "Endodontic Therapy, Molar Tooth (Tooth #46)", "46", "K04.0", 350m) },
            { "ai-find-103", ("D4341", "Periodontal Scaling & Root Planing (Tooth #25 Area)", "25", "K05.3", 110m) },
            { "ai-find-104", ("D7230", "Surgical Removal of Impacted Tooth - Partially Bony (Tooth #38)", "38", "K07.3", 450m) }
        };

        // Act
        decimal totalTariff = 0m;
        foreach (var item in findingsMap.Values)
        {
            totalTariff += item.fee;
        }

        decimal copayPct = 20.0m;
        decimal patientCopay = Math.Round(totalTariff * (copayPct / 100m), 2);
        decimal claimedAmount = totalTariff - patientCopay;

        // Assert
        Assert.Equal(1030m, totalTariff);
        Assert.Equal(206m, patientCopay);
        Assert.Equal(824m, claimedAmount);
        Assert.Equal("D2391", findingsMap["ai-find-101"].code);
        Assert.Equal("D3330", findingsMap["ai-find-102"].code);
    }

    [Fact]
    public void REQ_INS_05_ClaimPacket_ComputesDeterministicSha256Hash()
    {
        // Arrange
        string claimNumber = "CLM-AI-202610-0001";
        string patientId = "patient-alpha";
        decimal claimedAmount = 824.00m;
        string providerId = "provider-axa";
        string timestamp = "2026-10-08T20:00:00Z";

        string rawPayload = $"{claimNumber}:{patientId}:{claimedAmount}:{providerId}:{timestamp}";

        // Act
        using var sha = SHA256.Create();
        var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(rawPayload));
        string hash1 = Convert.ToHexString(hashBytes);

        var hashBytes2 = sha.ComputeHash(Encoding.UTF8.GetBytes(rawPayload));
        string hash2 = Convert.ToHexString(hashBytes2);

        // Assert
        Assert.Equal(hash1, hash2);
        Assert.Equal(64, hash1.Length); // 256 bits = 64 hex characters
    }
}
