using System.Reflection;
using System.Runtime.Loader;

namespace AutoTool.Plugin.Host.Services;

internal sealed class PluginAssemblyLoadContext(string pluginAssemblyPath) : AssemblyLoadContext(isCollectible: true)
{
    private readonly AssemblyDependencyResolver _resolver = new(pluginAssemblyPath);

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (string.Equals(assemblyName.Name, "AutoTool.Plugin.Abstractions", StringComparison.Ordinal))
        {
            return null;
        }

        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        return assemblyPath is null ? null : LoadFromAssemblyPath(assemblyPath);
    }
}

