namespace OrthoFlowAI.Api.Storage;

/// <summary>
/// Stores uploaded patient files (photos, documents) OUTSIDE wwwroot so they can
/// never be downloaded by guessing a static URL. Files are addressed by a storage
/// key such as "photos/{clinicId}/{guid}.jpg".
/// </summary>
public class PrivateFileStore
{
    private readonly string _root;
    private readonly string _legacyWebRoot;

    public PrivateFileStore(IConfiguration configuration, IWebHostEnvironment env)
        : this(
            configuration["Storage:RootPath"] is { Length: > 0 } configured
                ? configured
                : Path.Combine(env.ContentRootPath, "App_Data", "files"),
            env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"))
    {
    }

    public PrivateFileStore(string root, string legacyWebRoot)
    {
        _root = Path.GetFullPath(root);
        _legacyWebRoot = Path.GetFullPath(legacyWebRoot);
    }

    /// <summary>Saves the stream and returns its storage key.</summary>
    public async Task<string> SaveAsync(Stream content, string category, string clinicId, string extension, CancellationToken ct = default)
    {
        var safeCategory = SanitizeSegment(category);
        var safeClinic = SanitizeSegment(string.IsNullOrWhiteSpace(clinicId) ? "unassigned" : clinicId);
        var safeExtension = SanitizeExtension(extension);

        var key = $"{safeCategory}/{safeClinic}/{Guid.NewGuid():N}{safeExtension}";
        var path = ResolvePath(key) ?? throw new InvalidOperationException("Invalid storage key.");

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var file = File.Create(path);
        await content.CopyToAsync(file, ct);
        return key;
    }

    /// <summary>
    /// Maps a storage key to a physical path, or null if the key would escape the
    /// storage root (path traversal). Keys starting with "/uploads/" are legacy
    /// files that were saved under wwwroot before this store existed.
    /// </summary>
    public string? ResolvePath(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;

        var (baseDir, relative) = key.StartsWith("/uploads/", StringComparison.Ordinal)
            ? (_legacyWebRoot, key.TrimStart('/'))
            : (_root, key);

        var full = Path.GetFullPath(Path.Combine(baseDir, relative.Replace('/', Path.DirectorySeparatorChar)));
        var prefix = baseDir.EndsWith(Path.DirectorySeparatorChar) ? baseDir : baseDir + Path.DirectorySeparatorChar;

        return full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? full : null;
    }

    private static string SanitizeSegment(string value)
    {
        var cleaned = new string(value.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray());
        return cleaned.Length == 0 ? "x" : cleaned;
    }

    private static string SanitizeExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension)) return string.Empty;
        var cleaned = new string(extension.TrimStart('.').Where(char.IsLetterOrDigit).Take(10).ToArray()).ToLowerInvariant();
        return cleaned.Length == 0 ? string.Empty : "." + cleaned;
    }
}
