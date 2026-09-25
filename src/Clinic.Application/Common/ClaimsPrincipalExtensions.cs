using System.Security.Claims;

namespace Clinic.Application.Common;

public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal? principal)
    {
        return principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public static string? GetDoctorId(this ClaimsPrincipal? principal)
    {
        return principal?.FindFirst("doctorId")?.Value 
            ?? principal?.FindFirst("DoctorId")?.Value;
    }

    public static string? GetClinicId(this ClaimsPrincipal? principal)
    {
        return principal?.FindFirst("clinicId")?.Value 
            ?? principal?.FindFirst("ClinicId")?.Value;
    }

    public static string? GetUserRole(this ClaimsPrincipal? principal)
    {
        return principal?.FindFirst(ClaimTypes.Role)?.Value;
    }

    public static string? GetEmail(this ClaimsPrincipal? principal)
    {
        return principal?.FindFirst(ClaimTypes.Email)?.Value;
    }
}
