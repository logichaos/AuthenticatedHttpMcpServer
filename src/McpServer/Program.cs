var builder = WebApplication.CreateBuilder(args)
    .AddLogging();

builder.Services.AddMcp();

var app = builder.Build();

app.UseLogging();

app.UseMcp();
app.MapGet("/", () => "this is working");
await app.RunAsync();