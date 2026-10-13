using OrthoFlowAI.Application.DTOs;

namespace OrthoFlowAI.Web.Services;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task LogoutAsync();
    Task<AuthUserInfo?> GetCurrentUserAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<string?> GetTokenAsync();
}
