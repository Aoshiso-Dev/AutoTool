using AutoTool.Plugin.Host.Models;

namespace AutoTool.Plugin.Host.Abstractions;

public interface IPluginStartupDiagnosticsCatalog
{
    IReadOnlyList<PluginStartupDiagnostics> GetDiagnostics();
}

