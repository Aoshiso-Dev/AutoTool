using AutoTool.Commands.DependencyInjection;
using AutoTool.Commands.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AutoTool.Automation.Runtime.Tests;

public class CommandDependencyResolverTests
{
    [Fact]
    public void TryResolve_KnownService_ReturnsTrueAndResolvedInstance()
    {
        using var provider = CreateProvider();
        var resolver = provider.GetRequiredService<ICommandDependencyResolver>();

        var resolved = resolver.TryResolve(typeof(IVariableStore), out var service);

        Assert.True(resolved);
        Assert.NotNull(service);
        Assert.IsAssignableFrom<IVariableStore>(service);
    }

    [Fact]
    public void TryResolve_TimeProvider_ReturnsSystemProviderByDefault()
    {
        using var provider = CreateProvider();
        var resolver = provider.GetRequiredService<ICommandDependencyResolver>();

        var resolved = resolver.TryResolve(typeof(TimeProvider), out var service);

        Assert.True(resolved);
        var timeProvider = Assert.IsAssignableFrom<TimeProvider>(service);
        Assert.Same(TimeProvider.System, timeProvider);
    }

    [Fact]
    public void TryResolve_CommandEventBus_ReturnsRegisteredInstance()
    {
        using var provider = CreateProvider();
        var resolver = provider.GetRequiredService<ICommandDependencyResolver>();

        var resolved = resolver.TryResolve(typeof(ICommandEventBus), out var service);

        Assert.True(resolved);
        Assert.NotNull(service);
        Assert.IsAssignableFrom<ICommandEventBus>(service);
    }

    [Fact]
    public void TryResolve_UnknownService_ReturnsFalse()
    {
        using var provider = CreateProvider();
        var resolver = provider.GetRequiredService<ICommandDependencyResolver>();

        var resolved = resolver.TryResolve(typeof(Uri), out var service);

        Assert.False(resolved);
        Assert.Null(service);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddCommandServices();
        services.AddSingleton<ICommandDependencyResolver, CommandDependencyResolver>();
        return services.BuildServiceProvider();
    }
}
