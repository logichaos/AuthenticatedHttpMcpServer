namespace AuthenticatedHttpMcpServer.Infrastructure;

public static partial class ApiBuilder
{
  public static IHostApplicationBuilder AddLoggingServices(this IHostApplicationBuilder builder)
  {
    builder.Logging
      .AddConsole()
      .SetMinimumLevel(LogLevel.Trace);

    return builder;
  }
}