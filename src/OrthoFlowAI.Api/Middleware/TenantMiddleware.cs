using System.Security.Claims;
using OrthoFlowAI.Infrastructure.Data;

namespace OrthoFlowAI.Api.Middleware;

/// <summary>
/// Reads the clinic id from the authenticated user's JWT and scopes the request's
/// DbContext to it. Must be registered AFTER UseAuthentication(), otherwise
/// context.User is still anonymous and no tenant is ever applied.
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var clinicId = context.User.FindFirstValue("ClinicId")
                           ?? context.User.FindFirstValue("clinic_id")
                           ?? context.User.FindFirstValue("TenantId");

            if (!string.IsNullOrEmpty(clinicId))
            {
                context.Items["ClinicId"] = clinicId;

                // Must be the request-scoped DbContext (the same instance the
                // controllers/services receive), not one from a new scope.
                var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
                dbContext.SetClinicId(clinicId);

                _logger.LogDebug("Tenant context set for ClinicId: {ClinicId}", clinicId);
            }
        }

        await _next(context);
    }
}
