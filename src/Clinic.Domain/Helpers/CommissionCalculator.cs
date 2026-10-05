using System;

namespace Clinic.Domain.Helpers;

public static class CommissionCalculator
{
    public static decimal CalculateNetCommission(decimal gross, decimal labFee, decimal ratePercent, string deductionType)
    {
        var rate = ratePercent / 100m;
        switch (deductionType)
        {
            case "BeforeCommission":
                // Net Revenue = Gross - LabFee; Commission = Net * Rate
                var netRev = Math.Max(0m, gross - labFee);
                return Math.Round(netRev * rate, 2);

            case "AfterCommission":
                // Commission = (Gross * Rate) - LabFee
                var grossComm = gross * rate;
                return Math.Max(0m, Math.Round(grossComm - labFee, 2));

            case "None":
            default:
                // Commission = Gross * Rate
                return Math.Round(gross * rate, 2);
        }
    }

    public static decimal CalculateLabFee(decimal gross, string category, string? description)
    {
        var desc = (description ?? string.Empty).ToLowerInvariant();
        if (desc.Contains("crown") || desc.Contains("bridge") || desc.Contains("zirconia") || desc.Contains("porcelain"))
        {
            return Math.Round(gross * 0.25m, 2); // 25% lab fee for crown/bridge
        }
        if (desc.Contains("denture") || desc.Contains("prosthetic"))
        {
            return Math.Round(gross * 0.30m, 2); // 30% lab fee
        }
        if (desc.Contains("implant") || category.Equals("Implantology", StringComparison.OrdinalIgnoreCase))
        {
            return Math.Round(gross * 0.20m, 2); // 20% lab fee
        }
        if (desc.Contains("aligner") || category.Equals("Orthodontics", StringComparison.OrdinalIgnoreCase))
        {
            return Math.Round(gross * 0.15m, 2); // 15% lab fee
        }
        return 0m;
    }

    public static string DeduceCategory(string? description, string? doctorSpecialization)
    {
        var desc = (description ?? string.Empty).ToLowerInvariant();
        if (desc.Contains("root canal") || desc.Contains("endo") || desc.Contains("nerve")) return "Endodontics";
        if (desc.Contains("crown") || desc.Contains("bridge") || desc.Contains("veneer") || desc.Contains("implant")) return "Implantology";
        if (desc.Contains("brace") || desc.Contains("aligner") || desc.Contains("ortho")) return "Orthodontics";
        if (desc.Contains("extract") || desc.Contains("surg") || desc.Contains("wisdom")) return "Surgery";
        if (desc.Contains("fill") || desc.Contains("composite") || desc.Contains("decay")) return "Restorative";
        if (desc.Contains("clean") || desc.Contains("scale") || desc.Contains("polish") || desc.Contains("hygiene")) return "Preventive";

        if (!string.IsNullOrEmpty(doctorSpecialization))
        {
            if (doctorSpecialization.Contains("Endo", StringComparison.OrdinalIgnoreCase)) return "Endodontics";
            if (doctorSpecialization.Contains("Ortho", StringComparison.OrdinalIgnoreCase)) return "Orthodontics";
            if (doctorSpecialization.Contains("Surg", StringComparison.OrdinalIgnoreCase)) return "Surgery";
            if (doctorSpecialization.Contains("Implant", StringComparison.OrdinalIgnoreCase)) return "Implantology";
        }

        return "General Consultation";
    }
}
