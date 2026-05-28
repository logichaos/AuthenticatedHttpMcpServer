namespace McpServer.Infrastructure;

public static partial class ApiBuilder
{
    /// <summary>
    /// Configures logging. ASP.NET Core provides console logging by default;
    /// additional providers can be added here.
    /// </summary>
    public static WebApplicationBuilder AddLogging(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
        return builder;
    }

    /// <summary>
    /// Placeholder for logging middleware. ASP.NET Core already logs requests
    /// via the configured <see cref="ILogger"/> providers.
    /// </summary>
    public static WebApplication UseLogging(this WebApplication app)
    {
        return app;
    }
}
