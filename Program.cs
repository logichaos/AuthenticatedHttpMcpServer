using ModelContextProtocol.AspNetCore;
using AuthenticatedHttpMcpServer;

var builder = WebApplication.CreateBuilder(args);

builder.Services
  .AddMcpServer()
  .AddAuthorizationFilters()
  .WithHttpTransport()
  .WithToolsFromAssembly();
  
builder.Services.AddHttpContextAccessor();

builder.Services.AddHealthChecks();
builder.AddLoggingServices();

builder.Services.AddAuthServices(builder.Environment);

var app = builder.Build();

app.UseAuthorization();
app
  .MapMcp("/mcp")
  .RequireAuthorization();

app.MapGet("/", () => "Hello World!");

app.Run();
app.MapHealthChecks("/health")
  .RequireAuthorization(ApiBuilder.AuthConstants.Policies.MrAwesome)
  .RequireRateLimiting(ApiBuilder.RateLimit.Policies.Fixed);
