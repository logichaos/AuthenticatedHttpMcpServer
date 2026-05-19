using AspNetCore.Authentication.ApiKey;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;


public static partial class ApiBuilder
{
  public static IServiceCollection AddAuthServices(this IServiceCollection services, IHostEnvironment environment)
  {
    ApiKeyEvents apiKeyEvents = new()
    {
      OnValidateKey = context =>
      {
        if (context.ApiKey == "Lifetime Subscription")
          context.ValidationSucceeded();
        else
          context.ValidationFailed();

        return Task.CompletedTask;
      }
    };

    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
      .AddApiKeyInHeader($"{ApiKeyDefaults.AuthenticationScheme}-Header", options =>
      {
        options.KeyName = "Ocp-Apim-Subscription-Key";
        options.Realm = "API";
        options.Events = apiKeyEvents;
      })
      .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
      {
        options.Authority = $"https://login.microsoftonline.com/{"myentraid"}/v2.0";

        if (environment.IsDevelopment())
        {

          options.TokenValidationParameters.RequireSignedTokens = false;
          options.TokenValidationParameters.ValidateIssuerSigningKey = false;
          options.TokenValidationParameters.SignatureValidator =
              (token, _) => new JsonWebToken(token);
        }

        options.Events = new JwtBearerEvents
        {
          OnMessageReceived = ctx =>
          {
            Console.WriteLine("➡️ Incoming Authorization header:");
            Console.WriteLine(ctx.Request.Headers["Authorization"]);
            return Task.CompletedTask;
          },

          OnAuthenticationFailed = ctx =>
          {
            Console.WriteLine("❌ Authentication failed:");
            Console.WriteLine(ctx.Exception.ToString());
            return Task.CompletedTask;
          },

          OnTokenValidated = ctx =>
          {
            Console.WriteLine("✅ Token validated:");
            Console.WriteLine("Name: " + ctx.Principal.Identity?.Name);

            foreach (var claim in ctx.Principal.Claims)
              Console.WriteLine($"Claim: {claim.Type} = {claim.Value}");

            return Task.CompletedTask;
          }
        };

      });

    services.AddAuthorization(options =>
    {
      options.AddPolicy("mrawesome", policy =>
      {
        policy.RequireAuthenticatedUser();
        policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireRole(["mcpcaller", "awesome"]);
      });
      options.AddPolicy("mcp_subscription", policy =>
      {
        policy.RequireAuthenticatedUser();
        policy.AuthenticationSchemes.Add($"{ApiKeyDefaults.AuthenticationScheme}-Header");
      });
    });

    return services;
  }
}