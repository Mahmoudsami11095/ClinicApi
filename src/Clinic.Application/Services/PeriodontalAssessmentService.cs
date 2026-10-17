using System;
using System.Collections.Generic;
using Clinic.Application.DTOs;

namespace Clinic.Application.Services;

public static class PeriodontalAssessmentService
{
    public static PerioStagingAssessmentDto EvaluatePeriodontalStaging(
        int patientAge,
        double boneLossPercentage,
        bool isSmoker,
        int cigarettesPerDay,
        double? hbA1c,
        List<PeriodontalToothDto> teeth)
    {
        int totalSites = 0;
        int totalBleeding = 0;
        int deepPockets = 0;
        int maxCal = 0;
        int maxPpd = 0;
        int furcationCount = 0;
        int mobileCount = 0;
        int diseasedTeethCount = 0;
        int presentTeethCount = 0;

        foreach (var tooth in teeth ?? new List<PeriodontalToothDto>())
        {
            if (tooth.IsMissing) continue;
            presentTeethCount++;

            if (tooth.FurcationGrade > 0) furcationCount++;
            if (tooth.MobilityGrade > 0) mobileCount++;

            bool toothHasDeepPocket = false;
            foreach (var site in tooth.Sites ?? new List<PeriodontalSiteDto>())
            {
                totalSites++;
                if (site.HasBleedingOnProbing) totalBleeding++;
                if (site.ProbingDepthMm > maxPpd) maxPpd = site.ProbingDepthMm;
                if (site.ClinicalAttachmentLossMm > maxCal) maxCal = site.ClinicalAttachmentLossMm;
                if (site.ProbingDepthMm >= 5)
                {
                    deepPockets++;
                    toothHasDeepPocket = true;
                }
            }
            if (toothHasDeepPocket || tooth.FurcationGrade >= 2)
            {
                diseasedTeethCount++;
            }
        }

        double bopPercent = totalSites > 0 ? Math.Round((totalBleeding / (double)totalSites) * 100.0, 1) : 0.0;

        // BR-PERIO-01: AAP/EFP 2018 Staging
        string stage;
        string stageDesc;
        if (maxCal == 0)
        {
            stage = bopPercent >= 10.0 ? "Gingivitis" : "Health";
            stageDesc = bopPercent >= 10.0
                ? "Biofilm-induced Gingivitis with intact periodontium."
                : "Pristine Clinical Gingival Health with zero attachment loss.";
        }
        else if (maxCal >= 5 || maxPpd >= 6 || furcationCount >= 2)
        {
            if (furcationCount >= 3 || mobileCount >= 4)
            {
                stage = "Stage IV";
                stageDesc = "Advanced Periodontitis with extensive tooth loss risk and severe masticatory dysfunction.";
            }
            else
            {
                stage = "Stage III";
                stageDesc = "Severe Periodontitis with potential for tooth loss. Deep pockets ≥ 6 mm, CAL ≥ 5 mm.";
            }
        }
        else if (maxCal >= 3 || maxPpd == 5)
        {
            stage = "Stage II";
            stageDesc = "Moderate Periodontitis. Maximum pocket depths 5 mm, CAL 3-4 mm.";
        }
        else
        {
            stage = "Stage I";
            stageDesc = "Initial / Mild Periodontitis. Maximum pocket depths 4 mm, CAL 1-2 mm.";
        }

        // BR-PERIO-01: AAP/EFP 2018 Grading
        double boneLossAgeRatio = Math.Round(boneLossPercentage / Math.Max(1, patientAge), 2);
        string grade;
        string gradeDesc;

        bool hasGradeCRisk = boneLossAgeRatio > 1.00 || (isSmoker && cigarettesPerDay >= 10) || (hbA1c.HasValue && hbA1c.Value >= 7.0);
        bool hasGradeBRisk = boneLossAgeRatio >= 0.25 || (isSmoker && cigarettesPerDay < 10) || (hbA1c.HasValue && hbA1c.Value < 7.0);

        if (hasGradeCRisk)
        {
            grade = "Grade C";
            gradeDesc = "Rapid rate of periodontal progression. High biological susceptibility or systemic risk factors (heavy smoking / diabetes HbA1c ≥ 7.0%).";
        }
        else if (hasGradeBRisk)
        {
            grade = "Grade B";
            gradeDesc = "Moderate rate of progression. Expected destruction commensurate with biofilm levels.";
        }
        else
        {
            grade = "Grade A";
            gradeDesc = "Slow rate of progression. Low biological susceptibility, non-smoker, normoglycemic.";
        }

        // BR-PERIO-02: Extent of Inflammation
        string extent;
        if (stage.StartsWith("Stage"))
        {
            double diseasedTeethPercent = presentTeethCount > 0 ? (diseasedTeethCount / (double)presentTeethCount) * 100.0 : 0.0;
            extent = diseasedTeethPercent > 30.0 ? "GeneralizedPeriodontitis" : "LocalizedPeriodontitis";
        }
        else
        {
            if (bopPercent < 10.0) extent = "Healthy";
            else if (bopPercent <= 30.0) extent = "LocalizedGingivitis";
            else extent = "GeneralizedGingivitis";
        }

        string riskSummary = $"{stage} ({grade}) - {extent}. BOP: {bopPercent:F1}%, Deep Pockets (≥5mm): {deepPockets}, Max CAL: {maxCal}mm.";
        string riskSummaryAr = $"{stage} ({grade}) - النطاق: {extent}. نزيف السبر: {bopPercent:F1}%، الجيوب العميقة (≥5مم): {deepPockets}، أقصى فقد ارتباط: {maxCal}مم.";

        return new PerioStagingAssessmentDto(
            Stage: stage,
            StageDescription: stageDesc,
            Grade: grade,
            GradeDescription: gradeDesc,
            BopPercentage: bopPercent,
            InflammationExtent: extent,
            MaxCalMm: maxCal,
            MaxPpdMm: maxPpd,
            TotalSitesExamined: totalSites,
            TotalBleedingSites: totalBleeding,
            DeepPocketsCount: deepPockets,
            FurcationTeethCount: furcationCount,
            MobileTeethCount: mobileCount,
            ClinicalRiskSummary: riskSummary,
            ClinicalRiskSummaryAr: riskSummaryAr
        );
    }
}
