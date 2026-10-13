using System.IO;
using System.Threading.Tasks;
using Clinic.Application.DTOs;

namespace Clinic.Application.Interfaces;

public interface IAiVoiceScribeService
{
    Task<ClinicalSoapNoteDto> AnalyzeConsultationAudioAsync(Stream? audioStream = null, string? fileName = null);
}
