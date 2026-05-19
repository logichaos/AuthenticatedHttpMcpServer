using AuthenticatedHttpMcpServer.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

GlobalConfigurations.ApiSettings = builder.Configuration.GetRequiredSection("ApiSettings").Get<SettingsModel>()!;

builder.Services
  .AddMcpServer()
  .AddAuthorizationFilters()
  .WithHttpTransport()
  .WithToolsFromAssembly();

builder.Services.AddHttpContextAccessor();
builder.Services.AddRateLimitServices();
builder.Services.AddHealthChecks();
builder.AddLoggingServices();

builder.Services.AddAuthServices(builder.Environment);

var app = builder.Build();

app.UseAuthorization();
app.UseRateLimiter();

app.MapMcp("/mcp")
  .RequireAuthorization()
  .RequireRateLimiting(ApiBuilder.RateLimit.Policies.Fixed);

app.MapHealthChecks("/health")
  .RequireAuthorization(ApiBuilder.AuthConstants.Policies.MrAwesome)
  .RequireRateLimiting(ApiBuilder.RateLimit.Policies.Fixed);

app.Run();