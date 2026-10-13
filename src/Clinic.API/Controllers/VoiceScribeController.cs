using System.Threading.Tasks;
using Clinic.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/voice-scribe")]
[Authorize(Roles = "doctor,assistant")]
public class VoiceScribeController : ControllerBase
{
    private readonly IAiVoiceScribeService _aiScribeService;

    public VoiceScribeController(IAiVoiceScribeService aiScribeService)
    {
        _aiScribeService = aiScribeService;
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzeAudio([FromForm] IFormFile? audio)
    {
        // In reality, this endpoint would stream the audio chunk or IFormFile to the AI Service.
        // For development/simulation, we accept it (even if null) and return the mock structure.
        using var stream = audio?.OpenReadStream();
        var result = await _aiScribeService.AnalyzeConsultationAudioAsync(stream, audio?.FileName);

        return Ok(new { data = result });
    }
}
