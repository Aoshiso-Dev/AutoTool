using AutoTool.Plugin.Host.Models;

namespace AutoTool.Plugin.Host.Abstractions;

public interface IPluginQuickActionCatalog
{
    IReadOnlyList<PluginQuickActionDescriptor> GetQuickActions();
}
