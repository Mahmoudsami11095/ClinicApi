using System.Collections.Generic;

namespace Clinic.Application.DTOs;

public class DicomMetadataDto
{
    public string RecordId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string StudyInstanceUid { get; set; } = string.Empty;
    public string SeriesInstanceUid { get; set; } = string.Empty;
    public string SopInstanceUid { get; set; } = string.Empty;
    public string Modality { get; set; } = "CBCT"; // "CBCT", "DX", "PX", "CT"
    public string StudyDescription { get; set; } = "Maxillofacial Volumetric CBCT & Panoramic Reconstruction";
    public string Manufacturer { get; set; } = "Carestream Dental / CS 9600 3D";
    public int Rows { get; set; } = 512;
    public int Columns { get; set; } = 512;
    public int BitsAllocated { get; set; } = 16;
    public int BitsStored { get; set; } = 12;
    public int HighBit { get; set; } = 11;
    public double RescaleIntercept { get; set; } = -1024.0;
    public double RescaleSlope { get; set; } = 1.0;
    public double WindowCenter { get; set; } = 500.0;
    public double WindowWidth { get; set; } = 2000.0;
    public int NumberOfFrames { get; set; } = 48;
    public double SliceThicknessMm { get; set; } = 0.5;
    public double PixelSpacingMm { get; set; } = 0.15;
    public double Kvp { get; set; } = 90.0;
    public double TubeCurrentMa { get; set; } = 8.0;
    public double ExposureTimeMs { get; set; } = 12000.0;
    public string PatientOrientation { get; set; } = "L/P";
    public List<HuPresetDto> HuPresets { get; set; } = new();
}

public class HuPresetDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public double WindowWidth { get; set; }
    public double WindowCenter { get; set; }
    public string ClinicalDescription { get; set; } = string.Empty;
}

public class DicomSliceDto
{
    public int SliceIndex { get; set; }
    public string Orientation { get; set; } = "Axial"; // "Axial", "Coronal", "Sagittal"
    public double SliceLocationMm { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string AnatomicalLandmark { get; set; } = string.Empty;
}

public class DicomSeriesDto
{
    public string RecordId { get; set; } = string.Empty;
    public string Modality { get; set; } = "CBCT";
    public int TotalSlices { get; set; }
    public List<DicomSliceDto> Slices { get; set; } = new();
}
