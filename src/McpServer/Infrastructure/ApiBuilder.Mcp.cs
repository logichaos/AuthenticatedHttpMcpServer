using McpServer.Tools;

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
    var endpoint = app.MapMcp();

    if (app.Services.IsOAuthConfigured())
    {
      endpoint.RequireAuthorization();
      endpoint.RequireCors(McpCorsPolicyName);
    }

    if (app.Services.IsRateLimiterConfigured())
    {
      endpoint.RequireRateLimiting(RateLimiterPolicyNames.McpRateLimits);
    }

    return app;
  }
}