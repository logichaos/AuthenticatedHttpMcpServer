using McpServer.Infrastructure;

var builder = WebApplication.CreateBuilder(args)
    .AddLogging();

builder.Services
  .ConfigureRateLimiter(builder.Configuration)
  .AddMcp();

var app = builder.Build();

app
  .UseLogging()
  .UseMcp()
  .UseMaps();
await app.RunAsync();