using System.ComponentModel;
using System.Reflection;
using System.Security.Claims;
using AuthenticatedHttpMcpServer.Infrastructure.ToolSelection;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Server;

namespace AuthenticatedHttpMcpServer.Unit.Tests;

public class ScopeToolsClaimsPrincipalToolSelectionStrategyTests
{
    private static readonly IReadOnlyCollection<McpServerTool> AllTools = BuildStubTools();

    private static IReadOnlyCollection<McpServerTool> BuildStubTools()
    {
        var target = new StubTools();
        return typeof(StubTools)
            .GetMethods()
            .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() is not null)
            .Select(m => McpServerTool.Create(m, target, new McpServerToolCreateOptions()))
            .ToArray();
    }

    private static ScopeToolsClaimsPrincipalToolSelectionStrategy CreateSut(HttpContext ctx)
    {
        var accessor = new HttpContextAccessor { HttpContext = ctx };
        return new ScopeToolsClaimsPrincipalToolSelectionStrategy(accessor);
    }

    private static HttpContext ContextWithScopes(params string[] scopes)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(
            new ClaimsIdentity(scopes.Select(s => new Claim("scope", s)), "test"));
        return context;
    }

    private static List<string> ToolNames(IEnumerable<McpServerTool> tools) =>
        tools.Select(t => t.ProtocolTool.Name).ToList();

    [Test]
    public async Task NoScopeClaims_ReturnsNoTools()
    {
        var result = CreateSut(ContextWithScopes()).FilterTools(AllTools);

        await Assert.That(result.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task UnauthenticatedUser_ReturnsNoTools()
    {
        var result = CreateSut(new DefaultHttpContext()).FilterTools(AllTools);

        await Assert.That(result.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task ToolAllScope_ReturnsEveryTool()
    {
        var result = CreateSut(ContextWithScopes("tool:all")).FilterTools(AllTools).ToList();

        await Assert.That(result.Count).IsEqualTo(AllTools.Count);
    }

    [Test]
    public async Task ToolAllScope_WithEmptyToolList_ReturnsEmpty()
    {
        var result = CreateSut(ContextWithScopes("tool:all")).FilterTools([]);

        await Assert.That(result.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task SingleToolScope_ReturnsOnlyThatTool()
    {
        var result = CreateSut(ContextWithScopes("tool:alpha")).FilterTools(AllTools).ToList();

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].ProtocolTool.Name).IsEqualTo("alpha");
    }

    [Test]
    public async Task MultipleToolScopes_ReturnsAllMatchedTools()
    {
        var result = CreateSut(ContextWithScopes("tool:alpha", "tool:gamma")).FilterTools(AllTools);

        await Assert.That(ToolNames(result)).IsEquivalentTo(["alpha", "gamma"]);
    }

    [Test]
    public async Task ScopeWithoutToolPrefix_DoesNotMatchAnyTool()
    {
        // "alpha" ≠ "tool:alpha"
        var result = CreateSut(ContextWithScopes("alpha")).FilterTools(AllTools);

        await Assert.That(result.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task ToolScopeForUnknownTool_ReturnsNoTools()
    {
        var result = CreateSut(ContextWithScopes("tool:does_not_exist")).FilterTools(AllTools);

        await Assert.That(result.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task MixedScopes_OnlyToolPrefixedOnesMatch()
    {
        // "openid" and "profile" are common OAuth scopes that must not leak tools
        var result = CreateSut(ContextWithScopes("openid", "profile", "tool:beta")).FilterTools(AllTools);

        await Assert.That(ToolNames(result)).IsEquivalentTo(["beta"]);
    }

    private sealed class StubTools
    {
        [McpServerTool(Name = "alpha"), Description("Alpha")]
        public string Alpha() => "";

        [McpServerTool(Name = "beta"), Description("Beta")]
        public string Beta() => "";

        [McpServerTool(Name = "gamma"), Description("Gamma")]
        public string Gamma() => "";
    }
}
