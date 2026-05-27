using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using TUnit.Core.Interfaces;

namespace McpServer.Integration.Tests;

/// <summary>
/// Sets environment to "Testing" so appsettings.Testing.json overrides
/// rate limits with low test-friendly values:
///   McpWindowRateLimit:  PermitLimit = 10
///   FixedWindowRateLimit: PermitLimit = 100
/// </summary>
public class McpRateLimitingWebApplicationFactory : WebApplicationFactory<Program>, IAsyncInitializer
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }

    public Task InitializeAsync()
    {
        _ = Server;
        return Task.CompletedTask;
    }
}
