namespace OrthoFlowAI.Infrastructure.Identity;

/// <summary>
/// A refresh token issued at login. Only a SHA-256 hash is stored, so a database leak
/// does not expose usable tokens. Tokens are single-use: refreshing revokes the old
/// one and links it to its replacement (rotation).
/// </summary>
public class RefreshToken
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByHash { get; set; }

    public bool IsActive(DateTime now) => RevokedAt is null && ExpiresAt > now;
}
