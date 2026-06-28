using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Clinic.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var statusCode = (int)HttpStatusCode.InternalServerError;
        var message = "An unexpected server error occurred.";
        
        if (exception is UnauthorizedAccessException)
        {
            statusCode = (int)HttpStatusCode.Unauthorized;
            message = exception.Message;
        }
        else if (exception is ArgumentException || exception is InvalidOperationException)
        {
            statusCode = (int)HttpStatusCode.BadRequest;
            message = exception.Message;
        }
        else if (exception is KeyNotFoundException)
        {
            statusCode = (int)HttpStatusCode.NotFound;
            message = exception.Message;
        }

        context.Response.StatusCode = statusCode;

        // TEMPORARY FOR DEBUGGING: Always show full error details
        var detail = exception.ToString();

        var response = new
        {
            message = message,
            detail = detail,
            status = statusCode
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(response, options);
        await context.Response.WriteAsync(json);
    }
}
