using AutoTool.Plugin.Abstractions.Interfaces;
using AutoTool.Plugin.Abstractions.PluginModel;

namespace AutoTool.Plugin.Host.Services;

internal sealed class PluginServiceRegistry : IPluginServiceRegistry
{
    private readonly List<PluginServiceRegistration> _registrations = [];

    public IReadOnlyList<PluginServiceRegistration> Registrations => _registrations;

    public void Register(Type serviceType, Type implementationType, PluginServiceLifetime lifetime)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(implementationType);

        _registrations.Add(new PluginServiceRegistration
        {
            ServiceType = serviceType,
            ImplementationType = implementationType,
            Lifetime = lifetime,
        });
    }

    public void RegisterInstance(Type serviceType, object instance)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(instance);

        _registrations.Add(new PluginServiceRegistration
        {
            ServiceType = serviceType,
            Instance = instance,
            Lifetime = PluginServiceLifetime.Singleton,
        });
    }
}

