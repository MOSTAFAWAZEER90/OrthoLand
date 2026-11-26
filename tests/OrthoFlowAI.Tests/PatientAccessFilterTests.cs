using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using OrthoFlowAI.Api.Security;

namespace OrthoFlowAI.Tests;

public class PatientAccessFilterTests
{
    private static AuthorizationFilterContext Run(string role, string controller, string action = "Get")
    {
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role) }, "Test")),
        };
        var descriptor = new ControllerActionDescriptor { ControllerName = controller, ActionName = action };
        var context = new AuthorizationFilterContext(
            new ActionContext(http, new RouteData(), descriptor), new List<IFilterMetadata>());
        new PatientAccessFilter().OnAuthorization(context);
        return context;
    }

    [Theory]
    [InlineData("Patients")]
    [InlineData("Invoices")]
    [InlineData("Appointments")]
    [InlineData("Lookups")]
    public void Patients_cannot_call_staff_endpoints(string controller)
    {
        var result = Assert.IsType<ObjectResult>(Run("Patient", controller).Result);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    [Theory]
    [InlineData("PatientPortal", "Get")]
    [InlineData("Auth", "Refresh")]
    [InlineData("Photos", "GetFile")]
    public void Patients_can_use_their_portal(string controller, string action)
    {
        Assert.Null(Run("Patient", controller, action).Result);
    }

    [Fact]
    public void Staff_are_not_affected()
    {
        Assert.Null(Run("ClinicOwner", "Patients").Result);
    }
}
