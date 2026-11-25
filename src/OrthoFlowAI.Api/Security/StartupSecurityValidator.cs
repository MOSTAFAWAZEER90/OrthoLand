using System.Text;

namespace OrthoFlowAI.Api.Security;

/// <summary>
/// Fails fast at startup when security-critical configuration is missing or still
/// uses a value that is public (committed to the repository).
/// </summary>
public static class StartupSecurityValidator
{
    public const int MinJwtKeyBytes = 32;

    /// <summary>Keys that have appeared in this repository and must never protect real data.</summary>
    public static readonly IReadOnlySet<string> PublicKeys = new HashSet<string>(StringComparer.Ordinal)
    {
        "OrthoFlowAI_SuperSecretKey_2024_MustBe32CharsLong!",
        "OrthoFlowAI_DEV_ONLY_Key_NotForProduction_0123456789",
        "OrthoFlowAI_DevSecretKey_2024_MustBe32Chars!!",
        "CHANGE_ME_generate_with_openssl_rand_base64_64",
    };

    public static IReadOnlyList<string> Validate(IConfiguration configuration, bool isDevelopment)
    {
        var errors = new List<string>();

        var jwtKey = configuration["Jwt:SecretKey"];
        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            errors.Add("Jwt:SecretKey is not set. Provide it via the Jwt__SecretKey environment variable or user-secrets.");
        }
        else
        {
            if (Encoding.UTF8.GetByteCount(jwtKey) < MinJwtKeyBytes)
                errors.Add($"Jwt:SecretKey must be at least {MinJwtKeyBytes} bytes.");

            if (!isDevelopment && PublicKeys.Contains(jwtKey))
                errors.Add("Jwt:SecretKey uses a value that is published in the repository. Generate a new one (openssl rand -base64 64).");
        }

        var signingKey = configuration["Storage:UrlSigningKey"];
        if (!isDevelopment && !string.IsNullOrEmpty(signingKey) && PublicKeys.Contains(signingKey))
            errors.Add("Storage:UrlSigningKey uses a value that is published in the repository.");

        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("DefaultConnection")))
            errors.Add("ConnectionStrings:DefaultConnection is not set.");

        if (!isDevelopment && configuration.GetValue<bool>("Seed:DemoData"))
            errors.Add("Seed:DemoData must not be enabled outside Development (it creates accounts with well-known passwords).");

        return errors;
    }

    public static void ValidateOrThrow(IConfiguration configuration, IHostEnvironment environment)
    {
        var errors = Validate(configuration, environment.IsDevelopment());
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Insecure or missing configuration:" + Environment.NewLine + " - " +
                string.Join(Environment.NewLine + " - ", errors));
        }
    }
}
