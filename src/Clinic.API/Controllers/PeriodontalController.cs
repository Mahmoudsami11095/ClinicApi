using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Clinic.Application.DTOs;
using Clinic.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PeriodontalController : ControllerBase
{
    private static readonly ConcurrentDictionary<string, PeriodontalExamDto> _latestExamsByPatient = new();

    static PeriodontalController()
    {
        // Seed initial periodontal exams for demonstration and testing
        var seedExam = GenerateDefaultExam("pat-201");
        _latestExamsByPatient["pat-201"] = seedExam;
    }

    [HttpGet("patients/{patientId}/latest")]
    [HttpGet("/api/dental/periodontal/patients/{patientId}/latest")]
    public ActionResult<PeriodontalExamDto> GetLatestExam(string patientId)
    {
        if (string.IsNullOrWhiteSpace(patientId))
        {
            return BadRequest(new { error = "Patient ID must be provided." });
        }

        var exam = _latestExamsByPatient.GetOrAdd(patientId, id => GenerateDefaultExam(id));
        return Ok(exam);
    }

    [HttpPost("patients/{patientId}")]
    [HttpPost("/api/dental/periodontal/patients/{patientId}")]
    public ActionResult<PeriodontalExamDto> SaveExam(string patientId, [FromBody] SavePeriodontalExamRequest request)
    {
        if (string.IsNullOrWhiteSpace(patientId))
        {
            return BadRequest(new { error = "Patient ID must be provided." });
        }

        if (request == null)
        {
            return BadRequest(new { error = "Exam request body cannot be null." });
        }

        var assessment = EvaluatePeriodontalStaging(
            patientAge: request.PatientAge > 0 ? request.PatientAge : 45,
            boneLossPercentage: request.BoneLossPercentage,
            isSmoker: request.IsSmoker,
            cigarettesPerDay: request.CigarettesPerDay,
            hbA1c: request.HbA1c,
            teeth: request.Teeth
        );

        var examDto = new PeriodontalExamDto(
            ExamId: $"perio-exam-{Guid.NewGuid():N}",
            PatientId: patientId,
            PatientAge: request.PatientAge > 0 ? request.PatientAge : 45,
            ExamDate: DateTime.UtcNow,
            ExaminerName: string.IsNullOrWhiteSpace(request.ExaminerName) ? "Dr. Specialist" : request.ExaminerName,
            IsSmoker: request.IsSmoker,
            CigarettesPerDay: request.CigarettesPerDay,
            HbA1c: request.HbA1c,
            BoneLossPercentage: request.BoneLossPercentage,
            Assessment: assessment,
            Teeth: request.Teeth ?? new List<PeriodontalToothDto>(),
            VoiceNotes: request.VoiceNotes
        );

        _latestExamsByPatient[patientId] = examDto;
        return Ok(examDto);
    }

    [HttpPost("calculate-staging")]
    [HttpPost("/api/dental/periodontal/calculate-staging")]
    public ActionResult<PerioStagingAssessmentDto> CalculateStaging([FromBody] CalculatePerioStagingRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Request cannot be null." });
        }

        var assessment = EvaluatePeriodontalStaging(
            patientAge: request.PatientAge > 0 ? request.PatientAge : 45,
            boneLossPercentage: request.BoneLossPercentage,
            isSmoker: request.IsSmoker,
            cigarettesPerDay: request.CigarettesPerDay,
            hbA1c: request.HbA1c,
            teeth: request.Teeth
        );

        return Ok(assessment);
    }

    public static PerioStagingAssessmentDto EvaluatePeriodontalStaging(
        int patientAge,
        double boneLossPercentage,
        bool isSmoker,
        int cigarettesPerDay,
        double? hbA1c,
        List<PeriodontalToothDto> teeth)
    {
        return PeriodontalAssessmentService.EvaluatePeriodontalStaging(
            patientAge, boneLossPercentage, isSmoker, cigarettesPerDay, hbA1c, teeth);
    }

    public static PeriodontalExamDto GenerateDefaultExam(string patientId)
    {
        var teeth = new List<PeriodontalToothDto>();
        // Generate standard 32 adult teeth
        for (int t = 1; t <= 32; t++)
        {
            bool isMolar = (t >= 1 && t <= 3) || (t >= 14 && t <= 19) || (t >= 30 && t <= 32);
            bool isDiseasedMolar = t == 19 || t == 30 || t == 14;

            int ppdDefault = isDiseasedMolar ? 6 : (isMolar ? 3 : 2);
            int gmDefault = isDiseasedMolar ? 1 : 0;
            int calDefault = ppdDefault + gmDefault;
            bool bopDefault = isDiseasedMolar;

            var sites = new List<PeriodontalSiteDto>
            {
                new("Distobuccal", ppdDefault, gmDefault, calDefault, bopDefault, false),
                new("Midbuccal", Math.Max(2, ppdDefault - 1), 0, Math.Max(2, ppdDefault - 1), false, false),
                new("Mesiobuccal", ppdDefault, gmDefault, calDefault, bopDefault, false),
                new("Distolingual", ppdDefault, gmDefault, calDefault, bopDefault, false),
                new("Midlingual", Math.Max(2, ppdDefault - 1), 0, Math.Max(2, ppdDefault - 1), false, false),
                new("Mesiolingual", ppdDefault, gmDefault, calDefault, bopDefault, false)
            };

            teeth.Add(new PeriodontalToothDto(
                ToothNumber: t,
                IsMissing: t == 1 || t == 16 || t == 17 || t == 32, // 3rd molars extracted
                IsImplant: t == 36 || t == 19,
                FurcationGrade: isDiseasedMolar ? 1 : 0,
                MobilityGrade: isDiseasedMolar ? 1 : 0,
                Sites: sites
            ));
        }

        var assessment = EvaluatePeriodontalStaging(
            patientAge: 48,
            boneLossPercentage: 35.0,
            isSmoker: false,
            cigarettesPerDay: 0,
            hbA1c: 5.6,
            teeth: teeth
        );

        return new PeriodontalExamDto(
            ExamId: $"perio-seed-{patientId}",
            PatientId: patientId,
            PatientAge: 48,
            ExamDate: DateTime.UtcNow,
            ExaminerName: "Dr. Emily Vance",
            IsSmoker: false,
            CigarettesPerDay: 0,
            HbA1c: 5.6,
            BoneLossPercentage: 35.0,
            Assessment: assessment,
            Teeth: teeth,
            VoiceNotes: "Recorded via VoiceScribe Chairside Dictation. Generalized chronic periodontitis in molars."
        );
    }
}
