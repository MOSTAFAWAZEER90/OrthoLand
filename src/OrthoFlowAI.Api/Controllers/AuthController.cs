using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrthoFlowAI.Api.Hubs;
using OrthoFlowAI.Application.DTOs;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Infrastructure.Services;
using AuthRegisterRequest = OrthoFlowAI.Application.DTOs.RegisterRequest;
using AuthLoginRequest = OrthoFlowAI.Application.DTOs.LoginRequest;
using InfraRegisterRequest = OrthoFlowAI.Infrastructure.Services.RegisterRequest;
using InfraLoginRequest = OrthoFlowAI.Infrastructure.Services.LoginRequest;

namespace OrthoFlowAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IEmailService emailService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResult>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] AuthRegisterRequest request)
    {
        _logger.LogInformation("Registration attempt for email: {Email}", request.Email);

        var clinicId = request.ClinicId ?? string.Empty;

        var infraRequest = new InfraRegisterRequest
        {
            Email = request.Email,
            Password = request.Password,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            Role = request.Role.ToString()
        };

        var result = await _authService.RegisterAsync(infraRequest, clinicId);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        _logger.LogInformation("User registered successfully: {Email}", request.Email);
        return CreatedAtAction(nameof(Register), new { id = result.Data!.User.Id }, result);
    }

    /// <summary>
    /// Authenticate user and return JWT token
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] AuthLoginRequest request)
    {
        _logger.LogInformation("Login attempt for email: {Email}", request.Email);

        var infraRequest = new InfraLoginRequest
        {
            Email = request.Email,
            Password = request.Password
        };

        var result = await _authService.LoginAsync(infraRequest);

        if (!result.Success)
        {
            return Unauthorized(result);
        }

        _logger.LogInformation("User logged in successfully: {Email}", request.Email);
        return Ok(result);
    }

    /// <summary>
    /// Refresh an expired JWT token
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        _logger.LogInformation("Token refresh attempt");

        var result = await _authService.RefreshTokenAsync(request.RefreshToken);

        if (!result.Success)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Logout current user and invalidate tokens
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse.FailResponse("User not identified"));
        }

        _logger.LogInformation("User logout: {UserId}", userId);

        var result = await _authService.LogoutAsync(userId);
        return Ok(result);
    }

    /// <summary>
    /// Change current user's password
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse.FailResponse("User not identified"));
        }

        _logger.LogInformation("Password change attempt for user: {UserId}", userId);

        if (request.NewPassword != request.ConfirmNewPassword)
        {
            return BadRequest(ApiResponse.FailResponse("New password and confirmation do not match"));
        }

        var resetRequest = new Infrastructure.Services.ResetPasswordRequest
        {
            Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            Token = string.Empty,
            NewPassword = request.NewPassword
        };

        var result = await _authService.ResetPasswordAsync(resetRequest);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        _logger.LogInformation("Password changed successfully for user: {UserId}", userId);
        return Ok(ApiResponse.SuccessResponse("Password changed successfully"));
    }

    /// <summary>
    /// Send a password reset email
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        _logger.LogInformation("Password reset requested for email: {Email}", request.Email);

        var result = await _authService.ForgotPasswordAsync(request.Email);
        return Ok(result);
    }
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
