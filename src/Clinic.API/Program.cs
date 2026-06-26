using Clinic.Application;
using Clinic.Infrastructure;
using Clinic.Infrastructure.Seed;
using System.Text.Json;
using System.Text.Json.Serialization;
using Clinic.API.Middleware;
using Clinic.API.Hubs;
using Clinic.Application.Interfaces;
using Clinic.API.Filters;

var builder = WebApplication.CreateBuilder(args);

// ── Application & Infrastructure (Clean Architecture) ──
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ── SignalR & Dispatchers ──
builder.Services.AddSignalR();
builder.Services.AddScoped<INotificationDispatcher, SignalRNotificationDispatcher>();

// ── Controllers ──
builder.Services.AddControllers(options => 
    {
        options.Filters.Add<AssistantClinicRequirementFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// ── Swagger ──
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── CORS ──
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:4200", "http://127.0.0.1:4200", "https://127.0.0.1:4200","http://localhost:4300","https://clinic-app-ten-topaz.vercel.app" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularApp", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// ── Seed Database ──
await DataSeeder.SeedAsync(app.Services);

// ── Global Exception Middleware ──
app.UseMiddleware<GlobalExceptionMiddleware>();

// ── Middleware Pipeline ──
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AngularApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.MapGet("/api/debug-error", () => 
{
    if (System.IO.File.Exists("startup-error.txt"))
        return Results.Text(System.IO.File.ReadAllText("startup-error.txt"));
    return Results.Ok("No startup errors found!");
});

app.Run();
