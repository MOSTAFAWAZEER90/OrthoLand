using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.JSInterop;

namespace OrthoFlowAI.Web.Services;

public class ApiService : IApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ApiService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IJSRuntime _js;
    private string? _authToken;

    public ApiService(IHttpClientFactory httpClientFactory, ILogger<ApiService> logger, IJSRuntime js)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _js = js;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
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
            var client = await CreateAuthorizedClientAsync();
            var response = await client.GetAsync(endpoint, ct);

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
        try
        {
            var client = await CreateAuthorizedClientAsync();
            var response = await client.PostAsJsonAsync(endpoint, data, _jsonOptions, ct);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct);
                return DeserializeResponse<T>(content);
            }

            var errorContent = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("API POST {Endpoint} returned {StatusCode}: {Error}", endpoint, response.StatusCode, errorContent);
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
            var client = await CreateAuthorizedClientAsync();
            var response = await client.PutAsJsonAsync(endpoint, data, _jsonOptions, ct);

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
            var client = await CreateAuthorizedClientAsync();
            var response = await client.DeleteAsync(endpoint, ct);

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
            var client = await CreateAuthorizedClientAsync();
            var response = await client.GetAsync(endpoint, ct);
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
