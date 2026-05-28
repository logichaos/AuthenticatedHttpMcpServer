namespace McpServer.Infrastructure;

public static partial class ApiBuilder_Maps
{
  public static WebApplication UseMaps(this WebApplication app)
  {
    var rootEndpoint = app.MapGet("/", () => "this is working");

    if (app.Services.IsRateLimiterConfigured())
    {
      app.UseRateLimiter();
      rootEndpoint.RequireRateLimiting(RateLimiterPolicyNames.Fixed);
    }

    return app;
  }
}