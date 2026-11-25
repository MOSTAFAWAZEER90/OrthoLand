using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using OrthoFlowAI.Domain.Common;

namespace OrthoFlowAI.Api.Security;

/// <summary>
/// Patients log in with the same JWT as staff and carry their clinic's id, so the tenant
/// filter alone let a patient call /api/patients, /api/invoices … and read every patient
/// of the clinic. Patients may only use their portal (and login/refresh, and signed
/// photo links); everything else returns 403.
/// </summary>
public sealed class PatientAccessFilter : IAuthorizationFilter
{
    private static readonly HashSet<string> AllowedForPatients = new(StringComparer.OrdinalIgnoreCase)
    {
        "PatientPortal",
        "Auth",
    };

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true || !user.IsInRole("Patient"))
            return;

        if (context.ActionDescriptor is not ControllerActionDescriptor action)
            return;

        // Signed photo files are anonymous by design (the signature is the permission).
        if (action.ControllerName == "Photos" && action.ActionName == "GetFile")
            return;

        if (!AllowedForPatients.Contains(action.ControllerName))
        {
            context.Result = new ObjectResult(ApiResponse.FailResponse("Not available for patient accounts."))
            {
                StatusCode = StatusCodes.Status403Forbidden,
            };
        }
    }
}
