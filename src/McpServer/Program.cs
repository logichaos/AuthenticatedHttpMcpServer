using McpServer.Infrastructure;

var builder = WebApplication.CreateBuilder(args)
    .AddLogging();

builder.Services
  .AddOAuth(builder.Configuration)
  .ConfigureRateLimiter(builder.Configuration)
  .AddMcp();

var app = builder.Build();

app
  .UseLogging()
  .UseOAuth()
  .UseMcp()
  .UseMaps();
await app.RunAsync();