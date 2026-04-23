using AutoTool.Plugin.Abstractions.PluginModel;

namespace AutoTool.Plugin.Host.Abstractions;

public interface IPluginManifestValidator
{
    IReadOnlyList<string> Validate(PluginManifest manifest, string pluginDirectoryPath);
}

