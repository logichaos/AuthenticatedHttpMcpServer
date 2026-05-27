using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Client;

namespace McpServer.Integration.Tests;

public class McpRateLimitingTests
{
    [ClassDataSource<McpRateLimitingWebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required McpRateLimitingWebApplicationFactory Factory { get; init; }

    // ──────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────

    private static async Task<McpClient> CreateMcpClientAsync(HttpClient httpClient)
    {
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = httpClient.BaseAddress!,
                TransportMode = HttpTransportMode.StreamableHttp,
            },
            httpClient,
            NullLoggerFactory.Instance,
            ownsHttpClient: false);

        return await McpClient.CreateAsync(transport);
    }

    // ──────────────────────────────────────────────────────────
    // MCP client can connect and interact
    // ──────────────────────────────────────────────────────────

    [Test]
    public async Task McpClient_CanConnectAndListTools()
    {
        var client = Factory.CreateClient();
        var mcpClient = await CreateMcpClientAsync(client);

        var tools = await mcpClient.ListToolsAsync();

        await Assert.That(tools).IsNotNull();
        await Assert.That(tools.Count).IsPositive();
    }

    // ──────────────────────────────────────────────────────────
    // MCP rate limit is enforced
    // ──────────────────────────────────────────────────────────

    [Test]
    public async Task McpClient_ThrowsWhenRateLimitExceeded()
    {
        var client = Factory.CreateClient();
        var mcpClient = await CreateMcpClientAsync(client);

        // Keep calling ListToolsAsync until rate-limited.
        // The test factory has McpWindowRateLimit.PermitLimit = 10,
        // so this takes at most a few iterations.
        var exception = await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            while (true)
                await mcpClient.ListToolsAsync();
        });

        await Assert.That((int)(exception!.StatusCode ?? 0))
            .IsEqualTo((int)HttpStatusCode.TooManyRequests);
    }

    // ──────────────────────────────────────────────────────────
    // Rate limit rejection body is included in the exception
    // ──────────────────────────────────────────────────────────

    [Test]
    public async Task McpClient_RateLimitExceptionContainsRejectionMessage()
    {
        var client = Factory.CreateClient();
        var mcpClient = await CreateMcpClientAsync(client);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            while (true)
                await mcpClient.ListToolsAsync();
        });

        // The SDK's EnsureSuccessStatusCodeWithResponseBodyAsync reads the
        // response body and includes it in the exception message.
        await Assert.That(exception!.Message)
            .Contains("Rate limit reached")
            .And.Contains("Retry after");
    }
}
