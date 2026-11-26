using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;
using OrthoFlowAI.Application.DTOs;

namespace OrthoFlowAI.Web.Services;

public class AuthService : IAuthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<AuthService> _logger;
    private readonly IApiService _apiService;
    private readonly JsonSerializerOptions _jsonOptions;

    public AuthService(
        IHttpClientFactory httpClientFactory,
        IJSRuntime jsRuntime,
        ILogger<AuthService> logger,
        IApiService apiService)
    {
        _httpClientFactory = httpClientFactory;
        _jsRuntime = jsRuntime;
        _logger = logger;
        _apiService = apiService;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("OrthoFlowAI.Api");
            var response = await client.PostAsJsonAsync("api/auth/login", request, _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var authResponse = ParseLoginResponse(content);

                if (authResponse?.Token != null)
                {
                    await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "authToken", authResponse.Token);
                    await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "refreshToken", authResponse.RefreshToken);
                    await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "userInfo",
                        JsonSerializer.Serialize(authResponse.User, _jsonOptions));
                    _apiService.SetAuthorizationHeader(authResponse.Token);
                }

                return authResponse;
            }

            _logger.LogWarning("Login failed: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return null;
        }
    }

    /// <summary>
    /// The API wraps the login result as { success, data: { accessToken, refreshToken,
    /// expiresAt, user } }. The Web used to deserialize it as a flat AuthResponse with a
    /// "token" field, so the token was always empty and login never succeeded.
    /// </summary>
    private AuthResponse? ParseLoginResponse(string content)
    {
        var envelope = JsonSerializer.Deserialize<LoginEnvelope>(content, _jsonOptions);
        var data = envelope?.Data;
        if (envelope?.Success != true || data == null || string.IsNullOrEmpty(data.AccessToken))
            return null;

        return new AuthResponse
        {
            Token = data.AccessToken,
            RefreshToken = data.RefreshToken,
            Expiration = data.ExpiresAt,
            User = data.User ?? new AuthUserInfo()
        };
    }

    private sealed class LoginEnvelope
    {
        public bool Success { get; set; }
        public LoginData? Data { get; set; }
    }

    private sealed class LoginData
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public AuthUserInfo? User { get; set; }
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "authToken");
            await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "refreshToken");
            await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "userInfo");
            _apiService.ClearAuthorizationHeader();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
        }
    }

    public async Task<AuthUserInfo?> GetCurrentUserAsync()
    {
        try
        {
            var token = await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", "authToken");
            if (!string.IsNullOrEmpty(token))
            {
                _apiService.SetAuthorizationHeader(token);
            }

            var userInfoJson = await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", "userInfo");
            if (string.IsNullOrEmpty(userInfoJson))
                return null;

            return JsonSerializer.Deserialize<AuthUserInfo>(userInfoJson, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await GetTokenAsync();
        return !string.IsNullOrEmpty(token);
    }

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", "authToken");
        }
        catch
        {
            return null;
        }
    }
}
