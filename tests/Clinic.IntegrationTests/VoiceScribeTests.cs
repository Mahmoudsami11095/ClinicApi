using System.Linq;
using System.Threading.Tasks;
using Clinic.API.Controllers;
using Clinic.Application.Interfaces;
using Clinic.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Clinic.UnitTests.Controllers;

public class VoiceScribeTests
{
    [Fact]
    public async Task REQ_VOICE_01_AnalyzeAudio_Returns_Structured_Soap_Note()
    {
        // Arrange
        IAiVoiceScribeService service = new AiVoiceScribeService();
        var controller = new VoiceScribeController(service);

        // Act
        var result = await controller.AnalyzeAudio(null!);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var responseObj = okResult.Value;
        Assert.NotNull(responseObj);
        
        var soapNote = (Clinic.Application.DTOs.ClinicalSoapNoteDto)responseObj.GetType().GetProperty("data")!.GetValue(responseObj, null)!;
        
        Assert.NotNull(soapNote.ChiefComplaint);
        Assert.Contains("lower right", soapNote.ChiefComplaint);
        Assert.Single(soapNote.OdontogramFindings);
        Assert.Equal("46", soapNote.OdontogramFindings.First().ToothNumber);
        Assert.Equal(2, soapNote.DraftPrescriptions.Count);
        Assert.Equal("Amoxicillin", soapNote.DraftPrescriptions.First().MedicationName);
        Assert.True(soapNote.ConfidenceScore > 0.90);
    }
}
