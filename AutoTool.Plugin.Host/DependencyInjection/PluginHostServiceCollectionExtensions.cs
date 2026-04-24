using AutoTool.Automation.Runtime.Definitions;
using AutoTool.Commands.DependencyInjection;
using AutoTool.Commands.Services;
using AutoTool.Plugin.Abstractions.PluginModel;
using AutoTool.Plugin.Host.Abstractions;
using AutoTool.Plugin.Host.Models;
using AutoTool.Plugin.Host.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AutoTool.Plugin.Host.DependencyInjection;

public static class PluginHostServiceCollectionExtensions
{
    public static IServiceCollection AddPluginHostServices(
        this IServiceCollection services,
        Action<PluginHostOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new PluginHostOptions();
        configure?.Invoke(options);

        var manifestValidator = new PluginManifestValidator();
        var manifestLoader = new PluginManifestLoader(manifestValidator);
        var catalogLoader = new PluginCatalogLoader(options, manifestLoader);
        var pluginLoader = new PluginLoader(catalogLoader);
        var loadedPluginCatalog = new LoadedPluginCatalog(pluginLoader);
        var pluginCommandCatalog = new PluginCommandCatalog(loadedPluginCatalog);
        var pluginQuickActionCatalog = new PluginQuickActionCatalog(loadedPluginCatalog, pluginCommandCatalog);
        var startupDiagnosticsCatalog = new PluginStartupDiagnosticsCatalog(loadedPluginCatalog, pluginCommandCatalog);

        ApplyPluginServiceRegistrations(services, loadedPluginCatalog.GetLoadedPlugins());
        _ = startupDiagnosticsCatalog.GetDiagnostics();

        services.AddSingleton(options);
        services.AddSingleton<IPluginManifestValidator>(manifestValidator);
        services.AddSingleton<IPluginManifestLoader>(manifestLoader);
        services.AddSingleton<IPluginCatalogLoader>(catalogLoader);
        services.AddSingleton<IPluginLoader>(pluginLoader);
        services.AddSingleton<ILoadedPluginCatalog>(loadedPluginCatalog);
        services.AddSingleton<IPluginCommandCatalog>(pluginCommandCatalog);
        services.AddSingleton<IPluginQuickActionCatalog>(pluginQuickActionCatalog);
        services.AddSingleton<IPluginStartupDiagnosticsCatalog>(startupDiagnosticsCatalog);
        services.AddSingleton<IPluginCommandDispatcher, PluginCommandDispatcher>();
        services.AddSingleton<IAdditionalCommandDependencyResolver, PluginCommandDependencyResolver>();
        services.AddSingleton<ICommandDependencyResolver>(serviceProvider => new CommandDependencyResolver(
            serviceProvider.GetRequiredService<IVariableStore>(),
            serviceProvider.GetRequiredService<IObjectDetector>(),
            serviceProvider.GetRequiredService<IPathResolver>(),
            serviceProvider.GetRequiredService<IImageMatcher>(),
            serviceProvider.GetRequiredService<IMouseInput>(),
            serviceProvider.GetRequiredService<IKeyboardInput>(),
            serviceProvider.GetRequiredService<IScreenCapturer>(),
            serviceProvider.GetRequiredService<IProcessLauncher>(),
            serviceProvider.GetRequiredService<IWindowService>(),
            serviceProvider.GetRequiredService<IOcrEngine>(),
            serviceProvider.GetServices<IAdditionalCommandDependencyResolver>(),
            serviceProvider.GetService<ICommandEventBus>(),
            serviceProvider.GetService<TimeProvider>()));
        services.AddSingleton<IExternalCommandMetadataProvider, PluginRuntimeCommandMetadataProvider>();

        return services;
    }

    private static void ApplyPluginServiceRegistrations(IServiceCollection services, IReadOnlyList<LoadedPlugin> loadedPlugins)
    {
        foreach (var plugin in loadedPlugins)
        {
            foreach (var registration in plugin.ServiceRegistrations)
            {
                services.Add(CreateServiceDescriptor(plugin, registration));
            }
        }
    }

    private static ServiceDescriptor CreateServiceDescriptor(LoadedPlugin plugin, PluginServiceRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(registration);

        if (registration.Instance is not null)
        {
            if (!registration.ServiceType.IsInstanceOfType(registration.Instance))
            {
                throw new InvalidOperationException(
                    $"プラグイン '{plugin.Manifest.PluginId}' のサービス登録は serviceType に代入できない instance を返しました: {registration.ServiceType.FullName}");
            }

            return ServiceDescriptor.Singleton(registration.ServiceType, registration.Instance);
        }

        var implementationType = registration.ImplementationType
            ?? throw new InvalidOperationException(
                $"プラグイン '{plugin.Manifest.PluginId}' のサービス登録に implementationType または instance が必要です: {registration.ServiceType.FullName}");

        if (!registration.ServiceType.IsAssignableFrom(implementationType))
        {
            throw new InvalidOperationException(
                $"プラグイン '{plugin.Manifest.PluginId}' の implementationType は serviceType を実装していません: {registration.ServiceType.FullName} <= {implementationType.FullName}");
        }

        return registration.Lifetime switch
        {
            PluginServiceLifetime.Singleton => ServiceDescriptor.Singleton(registration.ServiceType, implementationType),
            PluginServiceLifetime.Scoped => ServiceDescriptor.Scoped(registration.ServiceType, implementationType),
            PluginServiceLifetime.Transient => ServiceDescriptor.Transient(registration.ServiceType, implementationType),
            _ => throw new InvalidOperationException(
                $"プラグイン '{plugin.Manifest.PluginId}' のサービス寿命が不正です: {registration.Lifetime}"),
        };
    }
}
