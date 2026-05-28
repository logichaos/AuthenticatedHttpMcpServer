using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace McpServer.Infrastructure.OAuth;

/// <summary>
/// Configures <see cref="JwtBearerOptions"/> for a specific OAuth provider type.
/// Implementations are stateless; register them via <see cref="OAuthSchemeRegistry"/>.
/// </summary>
public interface IOAuthSchemeConfigurator
{
    /// <summary>
    /// The provider type string this configurator handles (e.g. "InMemory", "EntraId", "Auth0").
    /// Must match the <c>Type</c> field in <see cref="OAuthSchemeConfig"/>.
    /// </summary>
    string ProviderType { get; }

    /// <summary>
    /// Applies provider-specific configuration to the JWT Bearer options.
    /// </summary>
    /// <param name="options">The JWT Bearer options to configure.</param>
    /// <param name="scheme">The scheme-level configuration from appsettings.</param>
    /// <param name="oauth">The top-level OAuth configuration.</param>
    void Configure(JwtBearerOptions options, OAuthSchemeConfig scheme, OAuthOptions oauth);
}
