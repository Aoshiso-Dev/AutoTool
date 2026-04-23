using AutoTool.Commands.DependencyInjection;
using AutoTool.Plugin.Host.Abstractions;

namespace AutoTool.Plugin.Host.Services;

/// <summary>
/// プラグインコマンド生成に必要な依存を CommandFactory へ提供します。
/// </summary>
public sealed class PluginCommandDependencyResolver(IPluginCommandDispatcher dispatcher) : IAdditionalCommandDependencyResolver
{
    private readonly IPluginCommandDispatcher _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

    public bool TryResolve(Type serviceType, out object? service)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        if (serviceType.IsInstanceOfType(_dispatcher))
        {
            service = _dispatcher;
            return true;
        }

        service = null;
        return false;
    }
}
