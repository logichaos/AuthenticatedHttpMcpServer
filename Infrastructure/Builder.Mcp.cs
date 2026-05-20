using AuthenticatedHttpMcpServer.Infrastructure.ToolSelection;

namespace AuthenticatedHttpMcpServer.Infrastructure;

public static partial class ApiBuilder
{
  public static IServiceCollection AddMcp(this IServiceCollection services)
  {
    services.AddSingleton<McpToolRegistry>();
    services.AddSingleton<HttpContextToolSelectionStrategy, ScopeToolsClaimsPrincipalToolSelectionStrategy>();

    services
      .AddMcpServer()
      .AddAuthorizationFilters()
      .WithHttpTransport(opts =>
      {
        opts.Stateless = false;

        opts.ConfigureSessionOptions = (ctx, mcpOpts, _) =>
        {
          var registry = ctx.RequestServices.GetRequiredService<McpToolRegistry>();
          var toolSelectionStrategy = ctx.RequestServices.GetRequiredService<HttpContextToolSelectionStrategy>();
          var userTools = registry.GetToolsForClaimsPrincipal(ctx, toolSelectionStrategy);
          mcpOpts.ToolCollection = [.. userTools];

          return Task.CompletedTask;
        };
      });

    return services;
  }
}