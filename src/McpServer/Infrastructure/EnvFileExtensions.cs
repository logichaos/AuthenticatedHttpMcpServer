namespace McpServer.Infrastructure;

/// <summary>
/// Loads key=value pairs from a .env file into the ASP.NET Core configuration system.
/// Supports the double-underscore convention (Key__SubKey) for hierarchical bindings,
/// so Mcp__OAuth__Schemes__EntraId__TenantId maps to Mcp:OAuth:Schemes:EntraId:TenantId.
/// </summary>
public static class EnvFileExtensions
{
    /// <summary>
    /// Adds a .env-style file to the configuration builder.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="path">Path to the .env file. Defaults to ".env" in the current directory.</param>
    /// <param name="optional">If true, skips silently when the file is missing.</param>
    public static IConfigurationBuilder AddEnvFile(
        this IConfigurationBuilder builder,
        string path = ".env",
        bool optional = true)
    {
        var fullPath = Path.GetFullPath(path);

        if (!File.Exists(fullPath))
        {
            if (!optional)
                throw new FileNotFoundException($".env file not found at '{fullPath}'.");
            return builder;
        }

        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in File.ReadLines(fullPath))
        {
            var trimmed = line.Trim();

            // Skip blank lines and comments
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                continue;

            var eq = trimmed.IndexOf('=');
            if (eq < 0)
                continue;

            var key = trimmed[..eq].Trim();
            var value = trimmed[(eq + 1)..].Trim();

            // Remove optional surrounding quotes
            if (value.Length >= 2 &&
                ((value.StartsWith('"') && value.EndsWith('"')) ||
                 (value.StartsWith('\'') && value.EndsWith('\''))))
            {
                value = value[1..^1];
            }

            // Convert double-underscore to colon for hierarchical binding
            // (the same convention ASP.NET Core uses for environment variables)
            key = key.Replace("__", ":");

            data[key] = value;
        }

        builder.AddInMemoryCollection(data);
        return builder;
    }
}
