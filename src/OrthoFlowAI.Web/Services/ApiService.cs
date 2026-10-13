using System.Net.Http.Json;
using System.Text.Json;

namespace OrthoFlowAI.Web.Services;

public class ApiService : IApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ApiService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private string? _authToken;

    public ApiService(IHttpClientFactory httpClientFactory, ILogger<ApiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
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
            var client = CreateClient();
            var response = await client.GetAsync(endpoint, ct);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct);
                return JsonSerializer.Deserialize<T>(content, _jsonOptions);
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
            var client = CreateClient();
            var response = await client.PostAsJsonAsync(endpoint, data, _jsonOptions, ct);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct);
                return JsonSerializer.Deserialize<T>(content, _jsonOptions);
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
            var client = CreateClient();
            var response = await client.PutAsJsonAsync(endpoint, data, _jsonOptions, ct);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct);
                return JsonSerializer.Deserialize<T>(content, _jsonOptions);
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
            var client = CreateClient();
            var response = await client.DeleteAsync(endpoint, ct);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct);
                if (string.IsNullOrWhiteSpace(content))
                    return default;

                return JsonSerializer.Deserialize<T>(content, _jsonOptions);
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
            var client = CreateClient();
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
                return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
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
            var client = CreateClient();
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
