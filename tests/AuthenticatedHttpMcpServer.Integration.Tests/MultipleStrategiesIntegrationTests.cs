using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;

namespace AuthenticatedHttpMcpServer.Integration.Tests;

// Verifies "all registered strategies must agree before a tool is returned."
//
// Two strategies are active in the full pipeline:
//   ScopeToolsClaimsPrincipalToolSelectionStrategy (scope claims)
//   ToolsOptionsToolSelectionStrategy              (AllowedTools config)
//
// Four combinations with TestWebApplicationFactory (AllowedTools: ["random_number"]):
//   ┌──────────────┬──────────────┬────────────────┐
//   │ Scope result │ Options result│ Final result   │
//   ├──────────────┼──────────────┼────────────────┤
//   │ allows       │ allows       │ tool returned  │  tool:random_number + allowed
//   │ allows       │ blocks       │ not returned   │  tool:hello_world + blocked
//   │ blocks       │ allows       │ not returned   │  no scope claim  + allowed
//   │ blocks       │ blocks       │ not returned   │  no scope claim  + not allowed
//   └──────────────┴──────────────┴────────────────┘
public class MultipleStrategiesIntegrationTests
{
    [ClassDataSource<TestWebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required TestWebApplicationFactory Factory { get; init; }

    private async Task<IReadOnlyList<string>> GetToolNames(params Claim[] claims)
    {
        var transportOpts = Factory.Services
            .GetRequiredService<IOptions<HttpServerTransportOptions>>().Value;

        using var scope = Factory.Services.CreateScope();

        var ctx = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = ctx;

        var mcpOpts = new McpServerOptions();
        await transportOpts.ConfigureSessionOptions!(ctx, mcpOpts, CancellationToken.None);

        return mcpOpts.ToolCollection?.Select(t => t.ProtocolTool.Name).ToList() ?? [];
    }

    // Scope allows AND options allows → tool returned.
    [Test]
    public async Task ScopeAllows_OptionsAllows_ToolIsReturned()
    {
        // tool:random_number satisfies scope; "random_number" is in AllowedTools.
        var tools = await GetToolNames(new Claim("scope", "tool:random_number"));

        await Assert.That(tools).IsEquivalentTo(["random_number"]);
    }

    // Scope allows BUT options blocks → tool not returned.
    [Test]
    public async Task ScopeAllows_OptionsBlocks_ToolNotReturned()
    {
        // tool:hello_world satisfies scope, but "hello_world" is not in AllowedTools.
        var tools = await GetToolNames(new Claim("scope", "tool:hello_world"));

        await Assert.That(tools.Count).IsEqualTo(0);
    }

    // Options allows BUT scope blocks → tool not returned.
    [Test]
    public async Task OptionsAllows_ScopeBlocks_ToolNotReturned()
    {
        // "random_number" is in AllowedTools, but no scope claim grants it.
        var tools = await GetToolNames();

        await Assert.That(tools.Count).IsEqualTo(0);
    }

    // Scope blocks AND options blocks → tool not returned.
    [Test]
    public async Task ScopeBlocks_OptionsBlocks_ToolNotReturned()
    {
        // No scope claim; "hello_world" is also absent from AllowedTools.
        var tools = await GetToolNames(new Claim("scope", "tool:unknown_tool"));

        await Assert.That(tools.Count).IsEqualTo(0);
    }

    // Verifies both directions of asymmetry: scope allows A, options allows B → empty.
    [Test]
    public async Task ScopeAllows_OptionsAllows_DifferentTools_ReturnsEmptyIntersection()
    {
        // Scope grants only hello_world; AllowedTools contains only random_number.
        // Neither passes both filters → no tools returned.
        var tools = await GetToolNames(new Claim("scope", "tool:hello_world"));

        await Assert.That(tools.Count).IsEqualTo(0);
    }
}

// Same four-combination matrix with AllToolsWebApplicationFactory (AllowedTools = null).
// With options as a pass-through, scope alone determines which tools are returned.
public class MultipleStrategies_NullOptions_IntegrationTests
{
    [ClassDataSource<AllToolsWebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required AllToolsWebApplicationFactory Factory { get; init; }

    private async Task<IReadOnlyList<string>> GetToolNames(params Claim[] claims)
    {
        var transportOpts = Factory.Services
            .GetRequiredService<IOptions<HttpServerTransportOptions>>().Value;

        using var scope = Factory.Services.CreateScope();

        var ctx = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = ctx;

        var mcpOpts = new McpServerOptions();
        await transportOpts.ConfigureSessionOptions!(ctx, mcpOpts, CancellationToken.None);

        return mcpOpts.ToolCollection?.Select(t => t.ProtocolTool.Name).ToList() ?? [];
    }

    // Options is a pass-through (null) AND scope allows → tool returned.
    [Test]
    public async Task OptionsPassThrough_ScopeAllows_ToolIsReturned()
    {
        var tools = await GetToolNames(new Claim("scope", "tool:hello_world"));

        await Assert.That(tools).IsEquivalentTo(["hello_world"]);
    }

    // Options is a pass-through (null) BUT scope blocks → tool not returned.
    [Test]
    public async Task OptionsPassThrough_ScopeBlocks_ToolNotReturned()
    {
        var tools = await GetToolNames();

        await Assert.That(tools.Count).IsEqualTo(0);
    }

    // Both pass-through (tool:all) → all tools returned.
    [Test]
    public async Task OptionsPassThrough_ScopeAll_AllToolsReturned()
    {
        var tools = await GetToolNames(new Claim("scope", "tool:all"));

        await Assert.That(tools).IsEquivalentTo(["hello_world", "random_number"]);
    }
}
