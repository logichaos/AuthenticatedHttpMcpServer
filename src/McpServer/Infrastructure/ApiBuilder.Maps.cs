namespace McpServer.Infrastructure;

public static partial class ApiBuilder_Maps
{
  public static WebApplication UseMaps(this WebApplication app)
  {
    app.UseRateLimiter();

    app.MapGet("/", () => "this is working")
      .RequireRateLimiting(RateLimiterPolicyNames.Fixed);
    return app;
  }
}