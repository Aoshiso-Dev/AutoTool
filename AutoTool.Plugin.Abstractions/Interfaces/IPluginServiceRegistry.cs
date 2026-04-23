using AutoTool.Plugin.Abstractions.PluginModel;

namespace AutoTool.Plugin.Abstractions.Interfaces;

public interface IPluginServiceRegistry
{
    void Register(Type serviceType, Type implementationType, PluginServiceLifetime lifetime);

    void RegisterInstance(Type serviceType, object instance);
}


