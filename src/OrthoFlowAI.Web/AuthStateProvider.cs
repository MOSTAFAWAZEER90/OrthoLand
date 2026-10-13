using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace OrthoFlowAI.Web.Services;

public class AuthStateProvider : AuthenticationStateProvider
{
    private readonly IAuthService _authService;

    public AuthStateProvider(IAuthService authService)
    {
        _authService = authService;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var user = await _authService.GetCurrentUserAsync();

        if (user == null)
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        if (!string.IsNullOrEmpty(user.ClinicId))
            claims.Add(new Claim("ClinicId", user.ClinicId));

        if (!string.IsNullOrEmpty(user.BranchId))
            claims.Add(new Claim("BranchId", user.BranchId));

        var identity = new ClaimsIdentity(claims, "JwtAuth");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public void NotifyAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
