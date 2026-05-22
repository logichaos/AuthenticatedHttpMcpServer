using AuthenticatedHttpMcpServer.Infrastructure;
using AuthenticatedHttpMcpServer.Infrastructure.ToolSelection;
using Microsoft.Extensions.DependencyInjection;

namespace AuthenticatedHttpMcpServer.Unit.Tests;

public class AddMcpTests
{
    private static IServiceCollection CreateServicesWithMcp()
    {
        var services = new ServiceCollection();
        services.AddMcp();
        return services;
    }

    [Test]
    public async Task AddMcp_RegistersMcpToolRegistryAsSingleton()
    {
        var descriptor = CreateServicesWithMcp()
            .FirstOrDefault(sd => sd.ServiceType == typeof(McpToolRegistry));

        await Assert.That(descriptor).IsNotNull();
        await Assert.That(descriptor!.Lifetime).IsEqualTo(ServiceLifetime.Singleton);
    }

    [Test]
    public async Task AddMcp_RegistersBothToolSelectionStrategiesAsSingletons()
    {
        var descriptors = CreateServicesWithMcp()
            .Where(sd => sd.ServiceType == typeof(ToolSelectionStrategy))
            .ToList();

        await Assert.That(descriptors.Count).IsEqualTo(2);
        await Assert.That(descriptors.All(d => d.Lifetime == ServiceLifetime.Singleton)).IsTrue();
    }

    [Test]
    public async Task AddMcp_RegistersScopeToolsClaimsPrincipalStrategy()
    {
        var types = CreateServicesWithMcp()
            .Where(sd => sd.ServiceType == typeof(ToolSelectionStrategy))
            .Select(sd => sd.ImplementationType)
            .ToList();

        await Assert.That(types).Contains(typeof(ScopeToolsClaimsPrincipalToolSelectionStrategy));
    }

    [Test]
    public async Task AddMcp_RegistersToolsOptionsStrategy()
    {
        var types = CreateServicesWithMcp()
            .Where(sd => sd.ServiceType == typeof(ToolSelectionStrategy))
            .Select(sd => sd.ImplementationType)
            .ToList();

        await Assert.That(types).Contains(typeof(ToolsOptionsToolSelectionStrategy));
    }
}
