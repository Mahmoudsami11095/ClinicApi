using System.Collections.Generic;
using System.Threading.Tasks;
using Clinic.Application.DTOs;
using Clinic.Domain.Entities;

namespace Clinic.Application.Interfaces;

public interface ICdsEngineService
{
    Task<CdsEvaluationResponseDto> EvaluatePrescriptionSafetyAsync(CdsEvaluationRequestDto request);
    Task<List<DrugInteractionRule>> GetInteractionRulesAsync();
    PediatricDosingSuggestionDto CalculatePediatricDose(string drugName, double weightKg, int ageYears);
}
