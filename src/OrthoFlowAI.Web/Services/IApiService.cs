namespace OrthoFlowAI.Web.Services;

public interface IApiService
{
    Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default);
    Task<T?> PostAsync<T>(string endpoint, object? data = null, CancellationToken ct = default);
    Task<T?> PutAsync<T>(string endpoint, object? data = null, CancellationToken ct = default);
    Task<T?> DeleteAsync<T>(string endpoint, CancellationToken ct = default);
    Task<Stream?> GetStreamAsync(string endpoint, CancellationToken ct = default);
    Task<T?> PostMultipartAsync<T>(string endpoint, string fieldName, byte[] fileBytes, string fileName, string contentType, Dictionary<string, string>? formFields = null, CancellationToken ct = default);
    string GetFullUrl(string pathOrUrl);
    void SetAuthorizationHeader(string token);
    void ClearAuthorizationHeader();
}
