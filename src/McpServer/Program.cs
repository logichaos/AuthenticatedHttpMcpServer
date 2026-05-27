using Microsoft.AspNetCore.Builder;
var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/", () => "this is working");

await app.RunAsync();