using ModelContextProtocol.Server;

namespace AuthenticatedHttpMcpServer.Infrastructure.ToolSelection;

public interface ToolSelectionStrategy<in T>
{
  IEnumerable<McpServerTool> GetTools(T input, IReadOnlyCollection<McpServerTool> tools);
}
public interface HttpContextToolSelectionStrategy: ToolSelectionStrategy<HttpContext>;