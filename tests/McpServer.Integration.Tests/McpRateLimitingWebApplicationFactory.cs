using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using TUnit.Core.Interfaces;

namespace McpServer.Integration.Tests;

/// <summary>
/// Uses the "RateLimitTesting" environment: OAuth disabled, rate limiting
/// enabled with 1-second windows so counters reset between tests.
/// </summary>
public class McpRateLimitingWebApplicationFactory : WebApplicationFactory<Program>, IAsyncInitializer
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("RateLimitTesting");
    }

    public Task InitializeAsync()
    {
        _ = Server;
        return Task.CompletedTask;
    }
}
