using ModelContextProtocol.AspNetCore;
using testy;

var builder = WebApplication.CreateBuilder(args);

builder.Services
  .AddMcpServer()
  .AddAuthorizationFilters()
  .WithHttpTransport()
  .WithToolsFromAssembly();
  
builder.Services.AddHttpContextAccessor();

builder.AddLoggingServices();

builder.Services.AddAuthServices(builder.Environment);

var app = builder.Build();

app.UseAuthorization();
app
  .MapMcp("/mcp")
  .RequireAuthorization();

app.MapGet("/", () => "Hello World!");

app.Run();
