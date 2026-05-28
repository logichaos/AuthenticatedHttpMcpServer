using McpServer.Infrastructure.OAuth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Net.Http.Headers;
using ModelContextProtocol.AspNetCore.Authentication;

namespace McpServer.Infrastructure;

public static partial class ApiBuilder
{
    private const string McpCorsPolicyName = "McpBrowserClient";

    /// <summary>
    /// Marker type registered in DI when OAuth is configured.
    /// <see cref="UseMcp"/> and <see cref="UseOAuth"/> check for this
    /// rather than relying on static state that leaks between test fixtures.
    /// </summary>
    internal sealed class OAuthMarker;

    /// <summary>
    /// Checks whether OAuth was configured for the given application instance.
    /// </summary>
    public static bool IsOAuthConfigured(this IServiceProvider services) =>
        services.GetService<OAuthMarker>() is not null;

    /// <summary>
    /// Registry of OAuth scheme configurators keyed by their <see cref="IOAuthSchemeConfigurator.ProviderType"/>.
    /// Add custom providers here or via <c>RegisterConfigurator</c> before calling <c>AddOAuth</c>.
    /// </summary>
    private static readonly Dictionary<string, IOAuthSchemeConfigurator> _configurators = new(StringComparer.OrdinalIgnoreCase)
    {
        [InMemoryOAuthConfigurator.ProviderTypeName] = new InMemoryOAuthConfigurator(),
        [EntraIdOAuthConfigurator.ProviderTypeName] = new EntraIdOAuthConfigurator(),
        [Auth0OAuthConfigurator.ProviderTypeName] = new Auth0OAuthConfigurator(),
    };

    /// <summary>
    /// Register a custom OAuth provider configurator. Call before <c>AddOAuth</c>.
    /// </summary>
    public static void RegisterOAuthConfigurator(IOAuthSchemeConfigurator configurator)
    {
        _configurators[configurator.ProviderType] = configurator;
    }

    /// <summary>
    /// Adds authentication and authorization services to the container.
    /// Reads all configuration from <c>Mcp:OAuth</c> in appsettings.
    ///
    /// Supports multiple named schemes registered simultaneously, with the default
    /// scheme selected via <c>OAuthOptions.DefaultScheme</c>.
    /// </summary>
    public static IServiceCollection AddOAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var oauthSection = configuration.GetSection(OAuthOptions.SectionName);
        if (!oauthSection.Exists())
        {
            // No OAuth section configured — skip auth entirely.
            // The app will run without authentication.
            return services;
        }

        var oauth = oauthSection.Get<OAuthOptions>()!;

        if (oauth.Schemes.Count == 0)
        {
            // OAuth section exists but has no schemes — skip auth.
            Console.WriteLine("Warning: OAuth section exists but has no schemes. Auth disabled.");
            return services;
        }

        var enabledSchemes = oauth.Schemes
            .Where(kvp => kvp.Value.Enabled)
            .ToList();

        if (enabledSchemes.Count == 0)
        {
            Console.WriteLine("Warning: No OAuth schemes are enabled. Auth disabled.");
            return services;
        }

        ValidateOptions(oauth, enabledSchemes);

        // Register a marker so UseOAuth/UseMcp can check if OAuth is configured
        // without relying on static state (which leaks between test fixtures).
        services.AddSingleton(new OAuthMarker());

        // ── CORS (from existing config) ─────────────────────────────
        var allowedOrigins = configuration
            .GetSection("Mcp:AllowedOrigins")
            .Get<string[]>() ?? ["http://localhost:5173"];

        services.AddCors(options =>
        {
            options.AddPolicy(McpCorsPolicyName, policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .WithMethods("POST")
                    .WithHeaders(HeaderNames.ContentType, HeaderNames.Authorization, "MCP-Protocol-Version")
                    .WithExposedHeaders(HeaderNames.WWWAuthenticate);
            });
        });

        // ── Authentication ──────────────────────────────────────────
        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;

            if (enabledSchemes.Count == 1)
            {
                options.DefaultAuthenticateScheme = enabledSchemes[0].Key;
            }
            else
            {
                // When multiple schemes are enabled, use a policy scheme that tries each
                // in order. The first successful validation wins.
                options.DefaultAuthenticateScheme = "MultiScheme";
            }
        });

        // If multiple schemes enabled, add a policy that forwards to all of them
        if (enabledSchemes.Count > 1)
        {
            authBuilder.AddPolicyScheme("MultiScheme", "Multi-Scheme JWT Bearer", options =>
            {
                options.ForwardDefaultSelector = _ =>
                {
                    // Try each enabled scheme in order; ASP.NET will use the first
                    // registered scheme as the fallback for the challenge.
                    return enabledSchemes[0].Key;
                };
            });
        }

        // Register each enabled scheme as a named JWT Bearer handler
        var authorizationServers = new List<string>();

        foreach (var (schemeName, schemeConfig) in enabledSchemes)
        {
            if (!_configurators.TryGetValue(schemeConfig.Type, out var configurator))
            {
                throw new InvalidOperationException(
                    $"Unknown OAuth provider type '{schemeConfig.Type}' for scheme '{schemeName}'. " +
                    $"Registered types: {string.Join(", ", _configurators.Keys)}. " +
                    "Register custom providers via ApiBuilder.RegisterOAuthConfigurator().");
            }

            var authority = ResolveAuthority(schemeConfig);
            if (authority is not null)
            {
                authorizationServers.Add(authority);
            }

            authBuilder.AddJwtBearer(schemeName, options =>
            {
                configurator.Configure(options, schemeConfig, oauth);
            });
        }

        // ── MCP authentication metadata ─────────────────────────────
        authBuilder.AddMcp(mcpOptions =>
        {
            mcpOptions.ResourceMetadata = new()
            {
                ResourceDocumentation = oauth.ResourceDocumentation,
                ScopesSupported = oauth.ScopesSupported ?? [],
            };

            foreach (var server in authorizationServers)
            {
                mcpOptions.ResourceMetadata.AuthorizationServers.Add(server);
            }
        });

        // ── Authorization ───────────────────────────────────────────
        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// Adds authentication and authorization middleware to the pipeline.
    /// </summary>
    public static WebApplication UseOAuth(this WebApplication app)
    {
        if (!app.Services.IsOAuthConfigured())
            return app;

        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private static void ValidateOptions(OAuthOptions oauth, List<KeyValuePair<string, OAuthSchemeConfig>> enabledSchemes)
    {
        if (string.IsNullOrEmpty(oauth.DefaultScheme))
        {
            throw new InvalidOperationException(
                $"'{OAuthOptions.SectionName}:DefaultScheme' must be set.");
        }

        var defaultEnabled = enabledSchemes.Any(s => s.Key == oauth.DefaultScheme);

        if (!defaultEnabled)
        {
            throw new InvalidOperationException(
                $"DefaultScheme '{oauth.DefaultScheme}' is not an enabled scheme. " +
                $"Enabled schemes: {string.Join(", ", enabledSchemes.Select(s => s.Key))}");
        }
    }

    /// <summary>
    /// Resolves the authority URL for a scheme. Used for MCP resource metadata.
    /// Falls back to <see cref="OAuthSchemeConfig.AuthorityUrl"/>, then Entra ID construction,
    /// then Auth0 construction.
    /// </summary>
    private static string? ResolveAuthority(OAuthSchemeConfig scheme)
    {
        if (!string.IsNullOrEmpty(scheme.AuthorityUrl))
            return scheme.AuthorityUrl;

        if (scheme.Type.Equals("EntraId", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrEmpty(scheme.TenantId))
        {
            var instance = scheme.Instance ?? "https://login.microsoftonline.com/";
            return $"{instance.TrimEnd('/')}/{scheme.TenantId}/v2.0";
        }

        if (scheme.Type.Equals("Auth0", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrEmpty(scheme.Domain))
        {
            return $"https://{scheme.Domain.TrimEnd('/')}/";
        }

        return null;
    }
}
