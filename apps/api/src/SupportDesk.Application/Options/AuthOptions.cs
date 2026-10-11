namespace SupportDesk.Application.Options;

/// <summary>
/// Bound to the "Auth" section of configuration. The signing key must come from
/// configuration or an environment variable; it is never hard-coded.
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>
    /// HMAC-SHA256 signing key. Must be at least 32 bytes. Read from config or
    /// the Auth__JwtKey environment variable; never commit a real one.
    /// </summary>
    public string JwtKey { get; set; } = string.Empty;

    /// <summary>Token issuer — the app's identity. Not sensitive.</summary>
    public string Issuer { get; set; } = "SupportDesk";

    /// <summary>Token audience — who the token is for. Not sensitive.</summary>
    public string Audience { get; set; } = "SupportDesk";

    /// <summary>How long a token stays valid. Documented in the README.</summary>
    public int TokenLifetimeHours { get; set; } = 8;
}