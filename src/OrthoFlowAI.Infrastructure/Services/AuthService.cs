using OrthoFlowAI.Infrastructure.Data;
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
    private readonly ApplicationDbContext _db;

    public AuthService(
        ApplicationUserManager userManager,
        IUnitOfWork unitOfWork,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger,
        ApplicationDbContext db)
    {
        _userManager = userManager;
        _unitOfWork = unitOfWork;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
        _db = db;
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

    /// <summary>
    /// Exchanges a stored, unexpired, unused refresh token for a new access + refresh token
    /// (rotation). Presenting a token that was already used is treated as theft: all of
    /// the user's sessions are revoked.
    /// </summary>
    public async Task<ApiResponse<AuthResult>> RefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return ApiResponse<AuthResult>.FailResponse("Invalid refresh token.");

        var now = DateTime.UtcNow;
        var hash = HashToken(refreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
        if (stored is null)
            return ApiResponse<AuthResult>.FailResponse("Invalid refresh token.");

        if (stored.RevokedAt is not null)
        {
            _logger.LogWarning("Reuse of a revoked refresh token for user {UserId}; revoking all sessions", stored.UserId);
            await RevokeAllAsync(stored.UserId, now);
            return ApiResponse<AuthResult>.FailResponse("Invalid refresh token.");
        }

        if (stored.ExpiresAt <= now)
            return ApiResponse<AuthResult>.FailResponse("Refresh token expired. Please log in again.");

        var user = await _userManager.FindByIdAsync(stored.UserId);
        if (user is null || !user.IsActive)
            return ApiResponse<AuthResult>.FailResponse("User not found or deactivated.");

        var result = await GenerateJwtTokenAsync(user);
        stored.RevokedAt = now;
        stored.ReplacedByHash = HashToken(result.RefreshToken);
        await _db.SaveChangesAsync();

        return ApiResponse<AuthResult>.SuccessResponse(result, "Token refreshed successfully.");
    }

    private async Task RevokeAllAsync(string userId, DateTime now)
    {
        var active = await _db.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null).ToListAsync();
        foreach (var t in active) t.RevokedAt = now;
        await _db.SaveChangesAsync();
    }

    internal static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public async Task<ApiResponse> LogoutAsync(string userId)
    {
        // Logout now really ends the session: no refresh token of this user works any more.
        await RevokeAllAsync(userId, DateTime.UtcNow);

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
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshExpirationInDays > 0 ? _jwtSettings.RefreshExpirationInDays : 7),
        });
        await _db.SaveChangesAsync();

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

}
