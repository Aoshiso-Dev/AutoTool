using AutoTool.Plugin.Abstractions.PluginModel;

namespace AutoTool.Plugin.Abstractions.Interfaces;

public interface IPluginExecutionContext
{
    DateTimeOffset GetLocalNow();

    void Log(string message);

    void ReportProgress(int progress);

    string? GetVariable(string name);

    void SetVariable(string name, string value);

    string ResolvePath(string path);

    ValueTask PublishAsync(PluginUiRequest request, CancellationToken cancellationToken);

    ValueTask WriteArtifactAsync(PluginArtifactRequest request, CancellationToken cancellationToken);
}


