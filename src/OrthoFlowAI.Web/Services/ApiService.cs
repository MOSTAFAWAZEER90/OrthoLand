using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace OrthoFlowAI.Web.Services;

public class ApiService : IApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ApiService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IJSRuntime _js;
    private readonly NavigationManager? _navigation;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private string? _authToken;

    public ApiService(IHttpClientFactory httpClientFactory, ILogger<ApiService> logger, IJSRuntime js,
        NavigationManager? navigation = null)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _js = js;
        _navigation = navigation;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            // Show and send times in clinic (Cairo) time; the API works in UTC.
            Converters = { new ClinicTimeJsonConverter() }
        };
    }

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient("OrthoFlowAI.Api");
        if (!string.IsNullOrEmpty(_authToken))
        {
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authToken);
        }
        return client;
    }

    /// <summary>
    /// Creates a client carrying the JWT. The token is kept per Blazor circuit, so after
    /// a full page load (login does NavigateTo(forceLoad: true)) it is gone and every
    /// call returned 401. Restore it from sessionStorage, where login saved it.
    /// </summary>
    private async Task<HttpClient> CreateAuthorizedClientAsync()
    {
        if (string.IsNullOrEmpty(_authToken))
        {
            try
            {
                _authToken = await _js.InvokeAsync<string?>("sessionStorage.getItem", "authToken");
            }
            catch (InvalidOperationException)
            {
                // JS interop is not available during static prerendering; the
                // interactive render that follows will retry.
            }
            catch (JSDisconnectedException)
            {
            }
        }
        return CreateClient();
    }

    /// <summary>
    /// Every API endpoint answers { success, message, data } and paged endpoints put
    /// { items, totalCount, pageNumber, pageSize } inside data. Many pages were written
    /// against other shapes (a flat PaginatedApiResponse with a list in data, or the bare
    /// DTO without the envelope), so they never received any data. Adapt here, once.
    /// </summary>
    internal T? DeserializeResponse<T>(string json)
    {
        var type = typeof(T);
        try
        {
            if (JsonNode.Parse(json) is JsonObject root && root.ContainsKey("success") && root.ContainsKey("data"))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition().Name.StartsWith("PaginatedApiResponse"))
                {
                    if (root["data"] is JsonObject page && page["items"] is JsonArray items)
                    {
                        root["data"] = items.DeepClone();
                        root["totalCount"] = page["totalCount"]?.DeepClone();
                        root["pageNumber"] = page["pageNumber"]?.DeepClone();
                        root["pageSize"] = page["pageSize"]?.DeepClone();
                        json = root.ToJsonString();
                    }
                }
                else if (!IsEnvelopeType(type))
                {
                    // Page expects the bare DTO: unwrap data.
                    json = root["data"]?.ToJsonString() ?? "null";
                }
            }
        }
        catch (JsonException)
        {
            // Not JSON we understand; let the normal deserializer report it.
        }

        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }

    private static bool IsEnvelopeType(Type type)
    {
        if (type == typeof(object) || type == typeof(JsonElement) || type == typeof(JsonNode) || type == typeof(JsonObject))
            return true;
        for (var t = type; t != null; t = t.BaseType)
        {
            var name = t.IsGenericType ? t.GetGenericTypeDefinition().Name : t.Name;
            if (name.StartsWith("ApiResponse") || name.StartsWith("PaginatedApiResponse"))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Sends a request with the access token. Access tokens live 60 minutes; on 401 the
    /// stored refresh token is exchanged once for a new pair and the request retried.
    /// If that fails the session is over, so the user is sent to the login page instead
    /// of seeing empty screens.
    /// </summary>
    private async Task<HttpResponseMessage> SendAsync(Func<HttpClient, Task<HttpResponseMessage>> send)
    {
        var client = await CreateAuthorizedClientAsync();
        var tokenUsed = _authToken;
        var response = await send(client);
        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
            return response;

        if (!string.IsNullOrEmpty(tokenUsed) && await TryRefreshAsync(tokenUsed))
        {
            response.Dispose();
            return await send(CreateClient());
        }

        await EndSessionAsync();
        return response;
    }

    private async Task<bool> TryRefreshAsync(string expiredToken)
    {
        await _refreshLock.WaitAsync();
        try
        {
            // Another call on this circuit already refreshed while we waited.
            if (!string.IsNullOrEmpty(_authToken) && _authToken != expiredToken) return true;

            var refreshToken = await _js.InvokeAsync<string?>("sessionStorage.getItem", "refreshToken");
            if (string.IsNullOrEmpty(refreshToken)) return false;

            var response = await _httpClientFactory.CreateClient("OrthoFlowAI.Api")
                .PostAsJsonAsync("api/auth/refresh", new { refreshToken });
            if (!response.IsSuccessStatusCode) return false;

            var json = JsonNode.Parse(await response.Content.ReadAsStringAsync());
            var access = json?["data"]?["accessToken"]?.GetValue<string>();
            var refresh = json?["data"]?["refreshToken"]?.GetValue<string>();
            if (string.IsNullOrEmpty(access) || string.IsNullOrEmpty(refresh)) return false;

            await _js.InvokeVoidAsync("sessionStorage.setItem", "authToken", access);
            await _js.InvokeVoidAsync("sessionStorage.setItem", "refreshToken", refresh);
            _authToken = access;
            return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or JSException or HttpRequestException or JsonException)
        {
            _logger.LogWarning(ex, "Token refresh failed");
            return false;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task EndSessionAsync()
    {
        _authToken = null;
        try
        {
            await _js.InvokeVoidAsync("sessionStorage.removeItem", "authToken");
            await _js.InvokeVoidAsync("sessionStorage.removeItem", "refreshToken");
        }
        catch (Exception ex) when (ex is InvalidOperationException or JSException) { }

        if (_navigation != null && !_navigation.Uri.Contains("/login", StringComparison.OrdinalIgnoreCase))
        {
            var returnUrl = Uri.EscapeDataString(_navigation.ToBaseRelativePath(_navigation.Uri));
            _navigation.NavigateTo($"/login?returnUrl=/{returnUrl}", forceLoad: true);
        }
    }

    /// <summary>
    /// The API's explanation for the last failed POST (e.g. "The selected time slot is
    /// not available."), so dialogs can show the real reason instead of a generic error.
    /// </summary>
    public string? LastErrorMessage { get; private set; }

    private static string? ExtractMessage(string body)
    {
        try
        {
            if (JsonNode.Parse(body) is not JsonObject o) return null;
            var message = o["message"]?.GetValue<string>() ?? o["title"]?.GetValue<string>();
            // Model validation errors: { errors: { Field: ["msg"] } }
            if (o["errors"] is JsonObject fields)
            {
                var first = fields.Select(f => (f.Value as JsonArray)?.FirstOrDefault()?.GetValue<string>()).FirstOrDefault(m => m != null);
                if (first != null) message = first;
            }
            return string.IsNullOrWhiteSpace(message) ? null : message;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            return null;
        }
    }

    public void SetAuthorizationHeader(string token)
    {
        _authToken = token;
    }

    public void ClearAuthorizationHeader()
    {
        _authToken = null;
    }

    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default)
    {
        try
        {
            var response = await SendAsync(c => c.GetAsync(endpoint, ct));

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct);
                return DeserializeResponse<T>(content);
            }

            _logger.LogWarning("API GET {Endpoint} returned {StatusCode}", endpoint, response.StatusCode);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling GET {Endpoint}", endpoint);
            return default;
        }
    }

    public async Task<T?> PostAsync<T>(string endpoint, object? data = null, CancellationToken ct = default)
    {
        LastErrorMessage = null;
        try
        {
            var response = await SendAsync(c => c.PostAsJsonAsync(endpoint, data, _jsonOptions, ct));

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct);
                return DeserializeResponse<T>(content);
            }

            var errorContent = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("API POST {Endpoint} returned {StatusCode}: {Error}", endpoint, response.StatusCode, errorContent);
            LastErrorMessage = ExtractMessage(errorContent);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling POST {Endpoint}", endpoint);
            return default;
        }
    }

    public async Task<T?> PutAsync<T>(string endpoint, object? data = null, CancellationToken ct = default)
    {
        try
        {
            var response = await SendAsync(c => c.PutAsJsonAsync(endpoint, data, _jsonOptions, ct));

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct);
                return DeserializeResponse<T>(content);
            }

            _logger.LogWarning("API PUT {Endpoint} returned {StatusCode}", endpoint, response.StatusCode);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling PUT {Endpoint}", endpoint);
            return default;
        }
    }

    public async Task<T?> DeleteAsync<T>(string endpoint, CancellationToken ct = default)
    {
        try
        {
            var response = await SendAsync(c => c.DeleteAsync(endpoint, ct));

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct);
                if (string.IsNullOrWhiteSpace(content))
                    return default;

                return DeserializeResponse<T>(content);
            }

            _logger.LogWarning("API DELETE {Endpoint} returned {StatusCode}", endpoint, response.StatusCode);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling DELETE {Endpoint}", endpoint);
            return default;
        }
    }

    public async Task<T?> PostMultipartAsync<T>(string endpoint, string fieldName, byte[] fileBytes, string fileName, string contentType, Dictionary<string, string>? formFields = null, CancellationToken ct = default)
    {
        try
        {
            var client = await CreateAuthorizedClientAsync();
            using var content = new MultipartFormDataContent();

            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            content.Add(fileContent, fieldName, fileName);

            if (formFields != null)
            {
                foreach (var kv in formFields)
                {
                    content.Add(new StringContent(kv.Value ?? string.Empty), kv.Key);
                }
            }

            var response = await client.PostAsync(endpoint, content, ct);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(ct);
                return DeserializeResponse<T>(responseContent);
            }

            var errorContent = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("API POST multipart {Endpoint} returned {StatusCode}: {Error}", endpoint, response.StatusCode, errorContent);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling POST multipart {Endpoint}", endpoint);
            return default;
        }
    }

    public string GetFullUrl(string pathOrUrl)
    {
        if (string.IsNullOrWhiteSpace(pathOrUrl)) return string.Empty;
        if (pathOrUrl.StartsWith("http://") || pathOrUrl.StartsWith("https://")) return pathOrUrl;
        if (pathOrUrl.StartsWith('/'))
        {
            var client = CreateClient();
            var baseUrl = client.BaseAddress?.ToString().TrimEnd('/') ?? string.Empty;
            return $"{baseUrl}{pathOrUrl}";
        }
        return pathOrUrl;
    }

    public async Task<Stream?> GetStreamAsync(string endpoint, CancellationToken ct = default)
    {
        try
        {
            var response = await SendAsync(c => c.GetAsync(endpoint, ct));
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsStreamAsync(ct);

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling GET stream {Endpoint}", endpoint);
            return null;
        }
    }
}
