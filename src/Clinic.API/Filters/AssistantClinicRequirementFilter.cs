using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Clinic.API.Filters;

public class AssistantClinicRequirementFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var controllerName = context.RouteData.Values["controller"]?.ToString();
        var actionName = context.RouteData.Values["action"]?.ToString();
        if (controllerName == "Auth" || controllerName == "Notifications" || actionName == "AssignAssistant" || actionName == "RespondAssignment")
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
                var clinicIds = user.FindAll("clinicIds").Select(c => c.Value).ToList();
                var singleClinicId = user.FindFirst("clinicId")?.Value;
                if (!string.IsNullOrEmpty(singleClinicId) && !clinicIds.Contains(singleClinicId))
                {
                    clinicIds.Add(singleClinicId);
                }

                if (!clinicIds.Any())
                {
                    context.Result = new ObjectResult(new { message = "You must be assigned to at least one clinic to access this data." })
                    {
                        StatusCode = 403
                    };
                    return;
                }

                var requestedClinicId = context.HttpContext.Request.Query["clinicId"].ToString();
                if (!string.IsNullOrEmpty(requestedClinicId) && requestedClinicId != "all")
                {
                    if (!clinicIds.Contains(requestedClinicId))
                    {
                        context.Result = new ObjectResult(new { message = "You are not authorized to access data for the requested clinic." })
                        {
                            StatusCode = 403
                        };
                        return;
                    }
                }
            }
        }
        await next();
    }
}
