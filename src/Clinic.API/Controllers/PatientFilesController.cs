using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/patients/{patientId}/files")]
[Authorize]
public class PatientFilesController : ControllerBase
{
    private readonly string _uploadFolder;

    public PatientFilesController(IWebHostEnvironment env)
    {
        _uploadFolder = Path.Combine(env.ContentRootPath, "uploads");
    }

    [HttpGet]
    public IActionResult GetFiles(string patientId)
    {
        var patientFolder = Path.Combine(_uploadFolder, patientId);
        if (!Directory.Exists(patientFolder))
        {
            return Ok(new { data = Array.Empty<object>() });
        }

        var files = Directory.GetFiles(patientFolder)
            .Select(filePath =>
            {
                var fileInfo = new FileInfo(filePath);
                var sizeInMb = (double)fileInfo.Length / (1024 * 1024);
                string sizeStr;
                if (sizeInMb >= 0.1)
                {
                    sizeStr = $"{sizeInMb:F1} MB";
                }
                else
                {
                    sizeStr = $"{fileInfo.Length / 1024} KB";
                }

                return new
                {
                    Name = fileInfo.Name,
                    Size = sizeStr,
                    Date = fileInfo.CreationTime.ToString("MMM dd, yyyy")
                };
            })
            .ToList();

        return Ok(new { data = files });
    }

    [HttpPost]
    public async Task<IActionResult> UploadFile(string patientId, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded" });
        }

        var patientFolder = Path.Combine(_uploadFolder, patientId);
        if (!Directory.Exists(patientFolder))
        {
            Directory.CreateDirectory(patientFolder);
        }

        var filePath = Path.Combine(patientFolder, file.FileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var fileInfo = new FileInfo(filePath);
        var sizeInMb = (double)fileInfo.Length / (1024 * 1024);
        string sizeStr = sizeInMb >= 0.1 ? $"{sizeInMb:F1} MB" : $"{fileInfo.Length / 1024} KB";

        var result = new
        {
            Name = fileInfo.Name,
            Size = sizeStr,
            Date = fileInfo.CreationTime.ToString("MMM dd, yyyy")
        };

        return Ok(new { message = "Success", data = result });
    }

    [HttpGet("{fileName}")]
    public IActionResult DownloadFile(string patientId, string fileName)
    {
        var filePath = Path.Combine(_uploadFolder, patientId, fileName);
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound(new { message = "File not found" });
        }

        var bytes = System.IO.File.ReadAllBytes(filePath);
        var contentType = "application/octet-stream";
        return File(bytes, contentType, fileName);
    }

    [HttpDelete("{fileName}")]
    public IActionResult DeleteFile(string patientId, string fileName)
    {
        var filePath = Path.Combine(_uploadFolder, patientId, fileName);
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound(new { message = "File not found" });
        }

        System.IO.File.Delete(filePath);
        return Ok(new { message = "Success" });
    }
}
