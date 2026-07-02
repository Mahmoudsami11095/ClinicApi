using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace Clinic.API.Filters;

public class SubscriptionActiveFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var controllerName = context.RouteData.Values["controller"]?.ToString();
        var actionName = context.RouteData.Values["action"]?.ToString();

        // Bypass checks for auth, subscriptions, admin, health checks, and profile routes
        if (controllerName == "Auth" || 
            controllerName == "Subscriptions" || 
            controllerName == "AdminSettings" || 
            controllerName == "Notifications" ||
            actionName == "GetProfile" ||
            actionName == "UpdateProfile")
        {
            await next();
            return;
        }

        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated == true)
        {
            var role = user.FindFirst(ClaimTypes.Role)?.Value;
            if (role == "doctor")
            {
                var email = user.FindFirst(ClaimTypes.Email)?.Value;
                if (!string.IsNullOrEmpty(email))
                {
                    var doctorRepo = context.HttpContext.RequestServices.GetRequiredService<IDoctorRepository>();
                    var doctors = await doctorRepo.GetAllAsync();
                    var doctor = doctors.FirstOrDefault(d => string.Equals(d.Email, email, StringComparison.OrdinalIgnoreCase));

                    if (doctor != null)
                    {
                        var isExpired = false;

                        // Check Trial status
                        if (doctor.SubscriptionStatus == "Trial" && DateTime.UtcNow > doctor.TrialEndDate)
                        {
                            isExpired = true;
                            doctor.SubscriptionStatus = "Expired";
                            await doctorRepo.UpdateAsync(doctor);
                        }
                        // Check Active subscription status
                        else if (doctor.SubscriptionStatus == "Active" && doctor.SubscriptionEndDate.HasValue && DateTime.UtcNow > doctor.SubscriptionEndDate.Value)
                        {
                            isExpired = true;
                            doctor.SubscriptionStatus = "Expired";
                            await doctorRepo.UpdateAsync(doctor);
                        }
                        // Already marked Expired
                        else if (doctor.SubscriptionStatus == "Expired")
                        {
                            isExpired = true;
                        }

                        // Check if PendingApproval
                        if (doctor.SubscriptionStatus == "PendingApproval")
                        {
                            context.Result = new ObjectResult(new 
                            { 
                                message = "Your subscription payment is currently pending approval by the administrator. Please contact support or check back later.",
                                isPendingApproval = true
                            })
                            {
                                StatusCode = 402 // Payment Required
                            };
                            return;
                        }

                        if (isExpired)
                        {
                            context.Result = new ObjectResult(new 
                            { 
                                message = "Your subscription has expired. Please complete the setup fee and annual subscription payment to unlock account access.",
                                isExpired = true,
                                trialEndDate = doctor.TrialEndDate,
                                subscriptionEndDate = doctor.SubscriptionEndDate
                            })
                            {
                                StatusCode = 402 // Payment Required
                            };
                            return;
                        }
                    }
                }
            }
        }

        await next();
    }
}
