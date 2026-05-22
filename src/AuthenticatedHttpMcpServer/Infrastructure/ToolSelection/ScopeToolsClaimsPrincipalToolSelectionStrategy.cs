using System.Security.Claims;
using ModelContextProtocol.Server;

namespace AuthenticatedHttpMcpServer.Infrastructure.ToolSelection;

public class ScopeToolsClaimsPrincipalToolSelectionStrategy(IHttpContextAccessor httpContextAccessor)
  : ToolSelectionStrategy
{
  private const string Scope = "scope";
  private const string ToolPrefix = "tool:";

  public IEnumerable<McpServerTool> FilterTools(IEnumerable<McpServerTool> tools)
  {
    ClaimsPrincipal userPrincipal = httpContextAccessor.HttpContext!.User;
    if (userPrincipal.HasClaim(Scope, $"{ToolPrefix}all"))
    {
      foreach (McpServerTool tool in tools)
      {
        yield return tool;
      }

      yield break;
    }

    foreach (McpServerTool tool in tools)
    {
      if (userPrincipal.HasClaim(Scope, $"{ToolPrefix}{tool.ProtocolTool.Name}"))
      {
        yield return tool;
      }
    }
  }
}