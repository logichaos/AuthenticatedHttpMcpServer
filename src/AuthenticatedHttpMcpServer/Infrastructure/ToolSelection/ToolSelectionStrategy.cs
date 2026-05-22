using ModelContextProtocol.Server;

namespace AuthenticatedHttpMcpServer.Infrastructure.ToolSelection;

public interface ToolSelectionStrategy
{
  IEnumerable<McpServerTool> FilterTools(IEnumerable<McpServerTool> tools);
}