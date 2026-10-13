using System.Security.Claims;
using OrthoFlowAI.Infrastructure.Data;

namespace OrthoFlowAI.Api.Middleware;

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

                var scope = context.RequestServices.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbContext.SetClinicId(clinicId);

                _logger.LogDebug("Tenant context set for ClinicId: {ClinicId}", clinicId);
            }
        }

        await _next(context);
    }
}
