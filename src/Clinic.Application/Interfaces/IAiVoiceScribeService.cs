using System.IO;
using System.Threading.Tasks;
using Clinic.Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace Clinic.Application.Interfaces;

public interface IAiVoiceScribeService
{
    Task<ClinicalSoapNoteDto> AnalyzeConsultationAudioAsync(IFormFile audioFile);
}
