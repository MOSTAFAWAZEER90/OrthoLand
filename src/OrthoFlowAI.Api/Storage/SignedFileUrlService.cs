using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace OrthoFlowAI.Api.Storage;

/// <summary>
/// Issues short-lived HMAC-signed URLs for private files. Browsers load images with
/// a plain &lt;img src&gt; (no Authorization header), so the URL itself carries proof
/// that an authorized API call produced it, and stops working after it expires.
/// </summary>
public class SignedFileUrlService
{
    private readonly byte[] _key;
    private readonly TimeSpan _lifetime;
    private readonly TimeProvider _time;

    public SignedFileUrlService(IConfiguration configuration)
        : this(
            configuration["Storage:UrlSigningKey"] is { Length: > 0 } k
                ? k
                // Domain-separated derivation so a signed URL can never be used as a JWT signature.
                : "file-url-signing:" + (configuration["Jwt:SecretKey"] ?? string.Empty),
            TimeSpan.FromMinutes(configuration.GetValue("Storage:SignedUrlLifetimeMinutes", 60)),
            TimeProvider.System)
    {
    }

    public SignedFileUrlService(string key, TimeSpan lifetime, TimeProvider time)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentException("Signing key is required.", nameof(key));
        _key = Encoding.UTF8.GetBytes(key);
        _lifetime = lifetime;
        _time = time;
    }

    /// <summary>Returns e.g. "/api/photos/{id}/file?expires=...&amp;sig=...".</summary>
    public string CreateUrl(string resource, string id)
    {
        var expires = _time.GetUtcNow().Add(_lifetime).ToUnixTimeSeconds();
        var sig = Sign(resource, id, expires);
        return $"/api/{resource}/{Uri.EscapeDataString(id)}/file?expires={expires}&sig={sig}";
    }

    public bool IsValid(string resource, string id, long expires, string? sig)
    {
        if (string.IsNullOrEmpty(sig)) return false;
        if (_time.GetUtcNow().ToUnixTimeSeconds() > expires) return false;

        var expected = Encoding.ASCII.GetBytes(Sign(resource, id, expires));
        var actual = Encoding.ASCII.GetBytes(sig);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    private string Sign(string resource, string id, long expires)
    {
        var payload = Encoding.UTF8.GetBytes(
            string.Create(CultureInfo.InvariantCulture, $"{resource}\n{id}\n{expires}"));
        var hash = HMACSHA256.HashData(_key, payload);
        return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
