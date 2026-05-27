namespace McpServer.Infrastructure;
public static partial class ApiBuilder
{
  public static IServiceCollection AddMcp(this IServiceCollection services)
  {
    services
      .AddMcpServer()
      .WithHttpTransport(opts => opts.Stateless = true)
      .WithTools<RandomNumberTools>();

    return services;
  }
  
  public static WebApplication UseMcp(this WebApplication app)
  {
    app.MapMcp()
      .RequireRateLimiting(RateLimiterPolicyNames.McpRateLimits);
    return app;
  }
}