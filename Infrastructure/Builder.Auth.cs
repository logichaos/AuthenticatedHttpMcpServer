using System.Security.Claims;

using AspNetCore.Authentication.ApiKey;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;

namespace AuthenticatedHttpMcpServer.Infrastructure;

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

    _ = services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
      .AddApiKeyInHeader($"{ApiKeyDefaults.AuthenticationScheme}-Header", options =>
      {
        options.KeyName = AuthConstants.AzureApiKeyName;
        options.Realm = "API";
        options.Events = apiKeyEvents;
      })
      .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
      {
        var tvp = GlobalConfigurations.ApiSettings!.TokenValidation;
        options.TokenValidationParameters.ValidIssuers = tvp.ValidIssuers;
        options.TokenValidationParameters.ValidAudiences = tvp.ValidAudiences;
        
        if (environment.IsDevelopment())
        {
          options.TokenValidationParameters.RequireSignedTokens = false;
          options.TokenValidationParameters.ValidateIssuerSigningKey = false;
          options.TokenValidationParameters.SignatureValidator =
            (token, _) => new JsonWebToken(token);
        }
        else
        {
          options.Authority = $"https://login.microsoftonline.com/{"myentraid"}/v2.0";
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
            Console.WriteLine("Name: " + ctx.Principal?.Identity?.Name);

            if (ctx.Principal?.Claims is IEnumerable<Claim> claims)
            {
              foreach (var claim in claims)
                Console.WriteLine($"Claim: {claim.Type} = {claim.Value}");
            }

            return Task.CompletedTask;
          }
        };
      });

    services.AddAuthorization(options =>
    {
      options.AddPolicy(AuthConstants.Policies.MrAwesome, policy =>
      {
        policy.RequireAuthenticatedUser();
        policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireRole([AuthConstants.Roles.McpCaller, AuthConstants.Roles.Awesome]);
      });
      options.AddPolicy(AuthConstants.Policies.McpSubscription, policy =>
      {
        policy.RequireAuthenticatedUser();
        policy.AuthenticationSchemes.Add($"{ApiKeyDefaults.AuthenticationScheme}-Header");
      });
    });

    return services;
  }

  public static class AuthConstants
  {
    public const string AzureApiKeyName = "Ocp-Apim-Subscription-Key";

    public static class Policies
    {
      public const string MrAwesome = "mrawesome";
      public const string McpSubscription = "mcp_subscription";
    }

    public static class Roles
    {
      public const string McpCaller = "mcpcaller";
      public const string Awesome = "awesome";
    }
  }
}