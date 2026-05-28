using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using TUnit.Core.Interfaces;

namespace McpServer.Integration.Tests;

/// <summary>
/// Sets environment to "Testing" so appsettings.Testing.json configures
/// test-friendly values (no OAuth, relaxed rate limits, etc.).
/// </summary>
public class WebApplicationFactory : WebApplicationFactory<Program>, IAsyncInitializer
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
