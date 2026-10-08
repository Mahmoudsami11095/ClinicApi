using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/consents")]
[Authorize(Roles = "admin,doctor,assistant")]
public class InformedConsentsController : ControllerBase
{
    private readonly ClinicDbContext _context;
    private readonly IInformedConsentRepository _consentRepo;

    public InformedConsentsController(ClinicDbContext context, IInformedConsentRepository consentRepo)
    {
        _context = context;
        _consentRepo = consentRepo;
    }

    // ── 1. Statutory Clinical Consent Templates ──
    [HttpGet("templates")]
    public IActionResult GetTemplates()
    {
        var templates = new List<ConsentTemplateDto>
        {
            new()
            {
                ProcedureType = "DentalImplant",
                ProcedureName = "Endosseous Dental Implant Placement & Bone Augmentation",
                StandardRisks = new List<string>
                {
                    "Temporary or permanent paresthesia of inferior alveolar / lingual nerve",
                    "Implant osseointegration failure or early rejection requiring removal",
                    "Sinus membrane perforation or displacement of graft granules",
                    "Post-operative bleeding, hematoma, swelling, and temporary trismus"
                },
                DescriptionEn = "Informed consent for titanium/zirconia dental implant installation and alveolar ridge preservation.",
                DescriptionAr = "إقرار الموافقة المستنيرة لزراعة الأسنان وتطعيم العظام الصناعي."
            },
            new()
            {
                ProcedureType = "SurgicalExtraction",
                ProcedureName = "Surgical Extraction of Impacted / Fractured Tooth",
                StandardRisks = new List<string>
                {
                    "Paresthesia or altered sensation of the lower lip, chin, or tongue",
                    "Dry socket (Alveolar osteitis) with localized throbbing pain",
                    "Damage to adjacent restorations or roots during elevation",
                    "Maxillary tuberosity fracture or oroantral communication"
                },
                DescriptionEn = "Informed surgical extraction consent detailing anatomical nerve proximity and post-op care.",
                DescriptionAr = "إقرار الخلع الجراحي لضرس العقل المدفون أو الجذور المتآكلة."
            },
            new()
            {
                ProcedureType = "RootCanal",
                ProcedureName = "Endodontic Root Canal Therapy & Coronal Restoration",
                StandardRisks = new List<string>
                {
                    "Separation of microscopic endodontic instrument in calcified canals",
                    "Apical flare-up or acute tenderness following chemomechanical debridement",
                    "Risk of crown or root vertical fracture if not protected with full crown",
                    "Possibility of requiring surgical apicoectomy or retreatment"
                },
                DescriptionEn = "Consent for pulpal debridement, canal shaping, obturation, and post-endodontic crown.",
                DescriptionAr = "إقرار علاج جذور وأعصاب الأسنان وتركيب التيجان الواقية."
            },
            new()
            {
                ProcedureType = "Orthodontics",
                ProcedureName = "Comprehensive Orthodontic Appliance / Clear Aligners",
                StandardRisks = new List<string>
                {
                    "Apical root resorption or mild blunting during tooth movement",
                    "Decalcification or enamel white spots without strict oral hygiene",
                    "Relapse or unwanted crowding without lifetime retention compliance",
                    "Transient temporomandibular joint (TMJ) adaptation or tenderness"
                },
                DescriptionEn = "Consent for fixed orthodontic brackets or clear aligner therapy and retention protocols.",
                DescriptionAr = "إقرار تقويم الأسنان الثابت والشفاف والتثبيت الدائم."
            },
            new()
            {
                ProcedureType = "AestheticBotox",
                ProcedureName = "Facial Aesthetic Injectables & Neuromodulators",
                StandardRisks = new List<string>
                {
                    "Temporary localized bruising, edema, headache, or mild asymmetry",
                    "Transient eyelid or brow ptosis (resolves typically within 2-4 weeks)",
                    "Extremely rare vascular compromise requiring hyaluronidase reversal",
                    "Requirement for periodic maintenance touch-ups every 3 to 6 months"
                },
                DescriptionEn = "Informed consent for facial neuromuscular aesthetic injections and dermal volumizers.",
                DescriptionAr = "إقرار حقن البوتوكس والفيلر التجميلي ومحددات الإجراء."
            }
        };

        return Ok(new { data = templates });
    }

    // ── 2. Create Consent Document ──
    [HttpPost]
    public async Task<IActionResult> CreateConsent([FromBody] CreateInformedConsentDto dto)
    {
        var patient = await _context.Patients.FindAsync(dto.PatientId);
        if (patient == null)
            return NotFound(new { message = "Patient record not found." });

        var doctor = await _context.Doctors.FindAsync(dto.DoctorId);
        if (doctor == null)
            return NotFound(new { message = "Doctor record not found." });

        var clinic = await _context.Clinics.FindAsync(dto.ClinicId);
        if (clinic == null)
            return NotFound(new { message = "Clinic facility not found." });

        var docNumber = await _consentRepo.GetNextDocumentNumberAsync();

        var document = new InformedConsentDocument
        {
            Id = Guid.NewGuid().ToString(),
            DocumentNumber = docNumber,
            ClinicId = dto.ClinicId,
            PatientId = dto.PatientId,
            DoctorId = dto.DoctorId,
            AppointmentId = dto.AppointmentId,
            ProcedureType = dto.ProcedureType,
            ProcedureName = dto.ProcedureName,
            ToothNumber = dto.ToothNumber,
            ClinicalRiskDisclosures = JsonSerializer.Serialize(dto.ClinicalRiskDisclosures),
            SpecialMedicalCautions = dto.SpecialMedicalCautions,
            Status = "PendingSignature",
            CreatedAt = DateTime.UtcNow
        };

        await _consentRepo.AddAsync(document);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = document.Id }, MapToDto(document, clinic.Name, $"{patient.FirstName} {patient.LastName}", $"Dr. {doctor.FirstName} {doctor.LastName}"));
    }

    // ── 3. Query Consents ──
    [HttpGet]
    public async Task<IActionResult> GetConsents([FromQuery] string? clinicId, [FromQuery] string? patientId, [FromQuery] string? status)
    {
        var query = _context.InformedConsents
            .Include(c => c.Clinic)
            .Include(c => c.Patient)
            .Include(c => c.Doctor)
            .Where(c => !c.IsDeleted);

        if (!string.IsNullOrEmpty(clinicId) && clinicId != "all")
            query = query.Where(c => c.ClinicId == clinicId);

        if (!string.IsNullOrEmpty(patientId))
            query = query.Where(c => c.PatientId == patientId);

        if (!string.IsNullOrEmpty(status) && status != "all")
            query = query.Where(c => c.Status == status);

        var list = await query.OrderByDescending(c => c.CreatedAt).AsNoTracking().ToListAsync();

        return Ok(new { data = list.Select(c => MapToDto(c, c.Clinic?.Name, $"{c.Patient?.FirstName} {c.Patient?.LastName}", $"Dr. {c.Doctor?.FirstName} {c.Doctor?.LastName}")) });
    }

    // ── 4. Get Single Consent ──
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var doc = await _consentRepo.GetByIdAsync(id);
        if (doc == null)
            return NotFound(new { message = "Consent document not found." });

        return Ok(new { data = MapToDto(doc, doc.Clinic?.Name, $"{doc.Patient?.FirstName} {doc.Patient?.LastName}", $"Dr. {doc.Doctor?.FirstName} {doc.Doctor?.LastName}") });
    }

    // ── 5. Patient Touchscreen Signature Capture ──
    [HttpPut("{id}/sign-patient")]
    public async Task<IActionResult> SignPatientConsent(string id, [FromBody] SignPatientConsentDto dto)
    {
        var doc = await _consentRepo.GetByIdAsync(id);
        if (doc == null)
            return NotFound(new { message = "Consent document not found." });

        if (doc.Status == "ArchivedLocked")
            return BadRequest(new { message = "BR-CONSENT-03: Locked consent documents cannot be modified." });

        doc.PatientSignatureBase64 = dto.PatientSignatureBase64;
        doc.SignatoryName = dto.SignatoryName.Trim();
        doc.SignatoryRelationship = string.IsNullOrWhiteSpace(dto.SignatoryRelationship) ? "Self" : dto.SignatoryRelationship;
        doc.SignedAt = DateTime.UtcNow;
        doc.Status = "SignedByPatient";

        await _consentRepo.UpdateAsync(doc);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Patient signature captured successfully.", data = MapToDto(doc, doc.Clinic?.Name, $"{doc.Patient?.FirstName} {doc.Patient?.LastName}", $"Dr. {doc.Doctor?.FirstName} {doc.Doctor?.LastName}") });
    }

    // ── 6. Doctor Countersignature & Cryptographic Immutability Lock (BR-CONSENT-03) ──
    [HttpPut("{id}/countersign")]
    public async Task<IActionResult> DoctorCountersign(string id, [FromBody] DoctorCountersignDto dto)
    {
        var doc = await _consentRepo.GetByIdAsync(id);
        if (doc == null)
            return NotFound(new { message = "Consent document not found." });

        if (doc.Status != "SignedByPatient")
            return BadRequest(new { message = "Patient must sign the consent before doctor countersignature." });

        doc.DoctorSignatureBase64 = dto.DoctorSignatureBase64;
        doc.CountersignedAt = DateTime.UtcNow;
        doc.Status = "ArchivedLocked";

        // Compute permanent SHA-256 hash across document data + signatures
        doc.DocumentSha256Checksum = ComputeDocumentSha256(doc);

        await _consentRepo.UpdateAsync(doc);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Consent document countersigned and cryptographically locked.", data = MapToDto(doc, doc.Clinic?.Name, $"{doc.Patient?.FirstName} {doc.Patient?.LastName}", $"Dr. {doc.Doctor?.FirstName} {doc.Doctor?.LastName}") });
    }

    public static string ComputeDocumentSha256(InformedConsentDocument doc)
    {
        var rawData = $"{doc.DocumentNumber}|{doc.PatientId}|{doc.DoctorId}|{doc.ToothNumber}|{doc.ProcedureType}|{doc.ClinicalRiskDisclosures}|{doc.PatientSignatureBase64}|{doc.DoctorSignatureBase64}|{doc.CountersignedAt:O}";
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(rawData);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }

    private static InformedConsentResponseDto MapToDto(
        InformedConsentDocument doc,
        string? clinicName,
        string? patientName,
        string? doctorName)
    {
        List<string> risks = new();
        if (!string.IsNullOrEmpty(doc.ClinicalRiskDisclosures))
        {
            try { risks = JsonSerializer.Deserialize<List<string>>(doc.ClinicalRiskDisclosures) ?? new(); }
            catch { risks = new(); }
        }

        return new InformedConsentResponseDto
        {
            Id = doc.Id,
            DocumentNumber = doc.DocumentNumber,
            ClinicId = doc.ClinicId,
            ClinicName = clinicName ?? doc.ClinicId,
            PatientId = doc.PatientId,
            PatientName = patientName ?? doc.PatientId,
            DoctorId = doc.DoctorId,
            DoctorName = doctorName ?? doc.DoctorId,
            AppointmentId = doc.AppointmentId,
            ProcedureType = doc.ProcedureType,
            ProcedureName = doc.ProcedureName,
            ToothNumber = doc.ToothNumber,
            ClinicalRiskDisclosures = risks,
            SpecialMedicalCautions = doc.SpecialMedicalCautions,
            PatientSignatureBase64 = doc.PatientSignatureBase64,
            DoctorSignatureBase64 = doc.DoctorSignatureBase64,
            SignatoryName = doc.SignatoryName,
            SignatoryRelationship = doc.SignatoryRelationship,
            DocumentSha256Checksum = doc.DocumentSha256Checksum,
            Status = doc.Status,
            CreatedAt = doc.CreatedAt,
            SignedAt = doc.SignedAt,
            CountersignedAt = doc.CountersignedAt
        };
    }
}
