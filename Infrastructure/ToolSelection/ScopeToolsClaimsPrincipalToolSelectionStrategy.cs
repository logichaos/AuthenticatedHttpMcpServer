using ModelContextProtocol.Server;

namespace AuthenticatedHttpMcpServer.Infrastructure.ToolSelection;

public class ScopeToolsClaimsPrincipalToolSelectionStrategy : HttpContextToolSelectionStrategy
{
  public IEnumerable<McpServerTool> GetTools(HttpContext ctx, IReadOnlyCollection<McpServerTool> tools)
  {
    var userPrincipal = ctx.User;
    if (userPrincipal.HasClaim("scope", $"tool:all"))
    {
      foreach (var tool in tools) yield return tool;
      yield break;
    }
    
    foreach (var tool in tools)
    {
      if (userPrincipal.HasClaim("scope", $"tool:{tool.ProtocolTool.Name}"))
        yield return tool;
    }
  }
}