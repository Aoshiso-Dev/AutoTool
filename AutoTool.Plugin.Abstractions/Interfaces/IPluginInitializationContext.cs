namespace AutoTool.Plugin.Abstractions.Interfaces;

public interface IPluginInitializationContext
{
    string HostVersion { get; }

    string PluginDirectoryPath { get; }

    void Log(string message);
}


