namespace McpServer.Infrastructure.OAuth;

/// <summary>
/// Top-level OAuth configuration, bound from <c>Mcp:OAuth</c> in appsettings.
/// Supports multiple named authentication schemes, switched per environment.
/// </summary>
public sealed class OAuthOptions
{
    public const string SectionName = "Mcp:OAuth";

    /// <summary>
    /// Name of the scheme to use as the default for authenticating incoming requests.
    /// Must match a key in <see cref="Schemes"/> that has <c>Enabled = true</c>.
    /// </summary>
    public string DefaultScheme { get; set; } = string.Empty;

    /// <summary>
    /// The public URL of this MCP server. Used as the expected audience in JWT validation
    /// and as the resource server identifier in MCP protected-resource metadata.
    /// When empty, defaults to the first URL configured on the host.
    /// </summary>
    public string ServerUrl { get; set; } = string.Empty;

    /// <summary>
    /// OAuth scopes this MCP server requires. Exposed in the
    /// <c>.well-known/oauth-protected-resource</c> metadata.
    /// </summary>
    public string[] ScopesSupported { get; set; } = [];

    /// <summary>
    /// Optional human-readable URL documenting the protected resource.
    /// </summary>
    public string? ResourceDocumentation { get; set; }

    /// <summary>
    /// Named authentication schemes keyed by scheme name (e.g. "InMemory", "EntraId").
    /// Only schemes with <c>Enabled = true</c> are registered.
    /// </summary>
    public Dictionary<string, OAuthSchemeConfig> Schemes { get; set; } = new();
}

/// <summary>
/// Configuration for a single OAuth authentication scheme.
/// </summary>
public sealed class OAuthSchemeConfig
{
    /// <summary>
    /// Whether this scheme should be registered in the DI container.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Human-readable name shown in MCP resource metadata (optional).
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Identifies the provider type. Must match a registered <see cref="IOAuthSchemeConfigurator"/>.
    /// Built-in values: <c>"InMemory"</c>, <c>"EntraId"</c>, <c>"Auth0"</c>.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    // ── Common JWT options ──────────────────────────────────────────

    /// <summary>
    /// The OAuth authorization server URL (the <c>iss</c> claim value).
    /// Used to fetch the OpenID Connect discovery document.
    /// </summary>
    public string? AuthorityUrl { get; set; }

    /// <summary>
    /// Expected <c>aud</c> claim. Defaults to <see cref="OAuthOptions.ServerUrl"/>.
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>
    /// Expected <c>iss</c> claim. Defaults to <see cref="AuthorityUrl"/>.
    /// Only override when the issuer differs from the authority URL.
    /// </summary>
    public string? Issuer { get; set; }

    /// <summary>
    /// Disable SSL certificate validation on the backchannel to the OAuth server.
    /// <b>Development only — never enable in production.</b>
    /// </summary>
    public bool DisableBackchannelSslValidation { get; set; }

    // ── Provider-specific options ───────────────────────────────────

    /// <summary>
    /// Microsoft Entra ID tenant ID (GUID or domain).
    /// Used to construct the authority URL: <c>{Instance}/{TenantId}/v2.0</c>.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Microsoft Entra ID instance. Defaults to <c>https://login.microsoftonline.com</c>.
    /// </summary>
    public string? Instance { get; set; }

    /// <summary>
    /// Auth0 domain (e.g. <c>my-tenant.us.auth0.com</c>).
    /// Used to construct the authority URL: <c>https://{Domain}/</c>.
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// OAuth client / application ID. Used by Entra ID and Auth0.
    /// </summary>
    public string? ClientId { get; set; }
}
