using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Clinic.API.Filters;

public class AssistantClinicRequirementFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var controllerName = context.RouteData.Values["controller"]?.ToString();
        if (controllerName == "Auth" || controllerName == "Notifications")
        {
            await next();
            return;
        }

        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated == true)
        {
            var role = user.FindFirst(ClaimTypes.Role)?.Value;
            if (role == "assistant")
            {
                var clinicId = user.FindFirst("clinicId")?.Value;
                if (string.IsNullOrEmpty(clinicId))
                {
                    context.Result = new ObjectResult(new { message = "You must be assigned to a clinic to access this data." })
                    {
                        StatusCode = 403
                    };
                    return;
                }
            }
        }
        await next();
    }
}
