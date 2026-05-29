using McpServer.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Load local .env file (gitignored) for secrets like tenant IDs and client secrets.
// Overrides appsettings.json values when the same key is present.
builder.Configuration.AddEnvFile();

builder.AddLogging();

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