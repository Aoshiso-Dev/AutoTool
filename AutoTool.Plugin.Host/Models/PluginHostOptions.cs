namespace AutoTool.Plugin.Host.Models;

public sealed record PluginHostOptions
{
    public string RootDirectoryPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "Plugins");
}



