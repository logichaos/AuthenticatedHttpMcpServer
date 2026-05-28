using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using TUnit.Core.Interfaces;

namespace McpServer.Integration.Tests.OAuth;

/// <summary>
/// WebApplicationFactory that starts the in-memory TestOAuthServer
/// and uses the Development environment (which has InMemory OAuth
/// pre-configured pointing at localhost:7029).
/// </summary>
public sealed class OAuthWebApplicationFactory : WebApplicationFactory<Program>, IAsyncInitializer
{
    private const string OAuthServerUrl = "https://localhost:7029";

    private readonly ModelContextProtocol.TestOAuthServer.Program _oauthServer;
    private readonly CancellationTokenSource _oauthCts = new();

    public OAuthWebApplicationFactory()
    {
        _oauthServer = new ModelContextProtocol.TestOAuthServer.Program(
            kestrelTransport: null);
    }

    public async Task InitializeAsync()
    {
        // Start the TestOAuthServer (it binds to 7029)
        _ = _oauthServer.RunServerAsync(cancellationToken: _oauthCts.Token);
        await _oauthServer.ServerStarted.WaitAsync(TimeSpan.FromSeconds(15));

        // Set ValidResources to include the test server's expected URL
        _oauthServer.ValidResources = [
            "http://localhost",
            "http://localhost:7071",
            "http://localhost:5000",
            "http://localhost:5000/mcp",
        ];

        // Force the test server to start
        _ = Server;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development environment already has InMemory OAuth enabled
        // pointing at https://localhost:7029 with SSL validation disabled.
        builder.UseEnvironment("Development");
    }

    public override async ValueTask DisposeAsync()
    {
        _oauthCts.Cancel();
        try { await Task.Delay(200); } catch { }
        _oauthCts.Dispose();
        await base.DisposeAsync();
    }
}
