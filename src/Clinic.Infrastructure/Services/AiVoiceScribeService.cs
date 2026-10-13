using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;

namespace Clinic.Infrastructure.Services;

public class AiVoiceScribeService : IAiVoiceScribeService
{
    public async Task<ClinicalSoapNoteDto> AnalyzeConsultationAudioAsync(Stream? audioStream = null, string? fileName = null)
    {
        // Simulate processing delay
        await Task.Delay(1500);

        // BR-AI-VOICE-02: Simulate ephemeral in-memory processing.
        // In production, this would call Whisper + GPT-4.
        
        return new ClinicalSoapNoteDto
        {
            ChiefComplaint = "Patient reports spontaneous pain in lower right posterior quadrant.",
            HistoryOfPresentIllness = "Pain started 3 days ago, exacerbated by cold and sweet stimuli.",
            Assessment = "Irreversible pulpitis on tooth 46 with deep distal caries.",
            Plan = "Perform RCT on 46. Prescribed Amoxicillin and Ibuprofen.",
            RawTranscript = "Patient says they have severe pain on the bottom right side. Started about 3 days ago. It hurts when drinking cold water. Looking at tooth 46, there is a deep distal decay reaching the pulp. We will need to do a root canal treatment on 46. I am prescribing Amoxicillin 500mg three times a day for 5 days and Ibuprofen 400mg twice a day for pain.",
            ConfidenceScore = 0.98,
            OdontogramFindings = new List<ClinicalFindingDto>
            {
                new ClinicalFindingDto { ToothNumber = "46", Condition = "Deep Caries", Severity = "Severe" }
            },
            DraftPrescriptions = new List<DraftPrescriptionDto>
            {
                new DraftPrescriptionDto { MedicationName = "Amoxicillin", Dosage = "500mg", Frequency = "TID", Duration = "5 Days" },
                new DraftPrescriptionDto { MedicationName = "Ibuprofen", Dosage = "400mg", Frequency = "BID", Duration = "3 Days" }
            }
        };
    }
}
