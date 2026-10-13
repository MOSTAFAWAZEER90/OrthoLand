using System.Security.Claims;
using OrthoFlowAI.Domain.Common;

namespace OrthoFlowAI.Infrastructure.Services;

public interface IAuthService
{
    Task<ApiResponse<AuthResult>> RegisterAsync(RegisterRequest request, string clinicId);
    Task<ApiResponse<AuthResult>> LoginAsync(LoginRequest request);
    Task<ApiResponse<AuthResult>> RefreshTokenAsync(string refreshToken);
    Task<ApiResponse> LogoutAsync(string userId);
    Task<ApiResponse> ForgotPasswordAsync(string email);
    Task<ApiResponse> ResetPasswordAsync(ResetPasswordRequest request);
    Task<ApiResponse<UserInfo>> GetCurrentUserAsync(string userId);
    Task<ApiResponse<List<string>>> GetUserRolesAsync(string userId);
}

public class AuthResult
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserInfo User { get; set; } = null!;
}

public class UserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ClinicId { get; set; }
    public string? ProfileImage { get; set; }
}

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Role { get; set; } = Domain.Enums.UserRole.Patient.ToString();
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
