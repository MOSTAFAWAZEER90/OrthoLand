using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Domain.Enums;
using OrthoFlowAI.Domain.Interfaces;
using OrthoFlowAI.Infrastructure.Identity;
using OrthoFlowAI.Infrastructure.Settings;

namespace OrthoFlowAI.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationUserManager _userManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ApplicationUserManager userManager,
        IUnitOfWork unitOfWork,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _unitOfWork = unitOfWork;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public async Task<ApiResponse<AuthResult>> RegisterAsync(RegisterRequest request, string clinicId)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            return ApiResponse<AuthResult>.FailResponse("A user with this email already exists.");
        }

        var identityUser = new ApplicationIdentityUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.Phone,
            ClinicId = clinicId,
            Role = request.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(identityUser, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return ApiResponse<AuthResult>.FailResponse("Registration failed.", errors);
        }

        await _userManager.AddToRoleAsync(identityUser, request.Role);

        var domainUser = new ApplicationUser
        {
            Id = identityUser.Id,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            Role = Enum.Parse<UserRole>(request.Role),
            ClinicId = clinicId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.ApplicationUsers.AddAsync(domainUser);
        await _unitOfWork.CommitAsync();

        var token = await GenerateJwtTokenAsync(identityUser);

        _logger.LogInformation("User registered: {Email} with role {Role}", request.Email, request.Role);

        return ApiResponse<AuthResult>.SuccessResponse(token, "Registration successful.");
    }

    public async Task<ApiResponse<AuthResult>> LoginAsync(LoginRequest request)
    {
        var validationResult = await _userManager.ValidateAndSignInAsync(request.Email, request.Password);
        if (!validationResult.Succeeded)
        {
            return ApiResponse<AuthResult>.FailResponse(
                validationResult.IdentityResult.Errors.FirstOrDefault()?.Description ?? "Invalid credentials.");
        }

        var token = await GenerateJwtTokenAsync(validationResult.User);

        _logger.LogInformation("User logged in: {Email}", request.Email);

        return ApiResponse<AuthResult>.SuccessResponse(token, "Login successful.");
    }

    public async Task<ApiResponse<AuthResult>> RefreshTokenAsync(string refreshToken)
    {
        var principal = GetPrincipalFromExpiredToken(refreshToken);
        if (principal is null)
        {
            return ApiResponse<AuthResult>.FailResponse("Invalid refresh token.");
        }

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return ApiResponse<AuthResult>.FailResponse("Invalid token claims.");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || !user.IsActive)
        {
            return ApiResponse<AuthResult>.FailResponse("User not found or deactivated.");
        }

        var token = await GenerateJwtTokenAsync(user);
        return ApiResponse<AuthResult>.SuccessResponse(token, "Token refreshed successfully.");
    }

    public async Task<ApiResponse> LogoutAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is not null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
        }

        return ApiResponse.SuccessResponse("Logged out successfully.");
    }

    public async Task<ApiResponse> ForgotPasswordAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return ApiResponse.SuccessResponse("If the email exists, a reset link has been sent.");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        _logger.LogInformation("Password reset token generated for {Email}. Token: {Token}", email, token);

        return ApiResponse.SuccessResponse("If the email exists, a reset link has been sent.");
    }

    public async Task<ApiResponse> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return ApiResponse.FailResponse("Invalid request.");
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return ApiResponse.FailResponse("Password reset failed.", errors);
        }

        return ApiResponse.SuccessResponse("Password has been reset successfully.");
    }

    public async Task<ApiResponse<UserInfo>> GetCurrentUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return ApiResponse<UserInfo>.FailResponse("User not found.");
        }

        var roles = await _userManager.GetRolesAsync(user);

        var info = new UserInfo
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = roles.FirstOrDefault() ?? user.Role,
            ClinicId = user.ClinicId,
            ProfileImage = user.ProfileImage
        };

        return ApiResponse<UserInfo>.SuccessResponse(info);
    }

    public async Task<ApiResponse<List<string>>> GetUserRolesAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return ApiResponse<List<string>>.FailResponse("User not found.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        return ApiResponse<List<string>>.SuccessResponse(roles.ToList());
    }

    private async Task<AuthResult> GenerateJwtTokenAsync(ApplicationIdentityUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new("clinic_id", user.ClinicId ?? string.Empty),
            new("branch_id", user.BranchId ?? string.Empty)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var refreshToken = GenerateRefreshToken();

        return new AuthResult
        {
            AccessToken = tokenString,
            RefreshToken = refreshToken,
            ExpiresAt = expires,
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = roles.FirstOrDefault() ?? user.Role,
                ClinicId = user.ClinicId,
                ProfileImage = user.ProfileImage
            }
        };
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
            ValidIssuer = _jwtSettings.Issuer,
            ValidAudience = _jwtSettings.Audience,
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
            if (securityToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
