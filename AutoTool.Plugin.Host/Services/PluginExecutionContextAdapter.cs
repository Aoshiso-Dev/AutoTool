using AutoTool.Commands.Interface;
using AutoTool.Plugin.Abstractions.Interfaces;
using AutoTool.Plugin.Abstractions.PluginModel;

namespace AutoTool.Plugin.Host.Services;

internal sealed class PluginExecutionContextAdapter(ICommandExecutionContext innerContext) : IPluginExecutionContext
{
    private readonly ICommandExecutionContext _innerContext = innerContext ?? throw new ArgumentNullException(nameof(innerContext));

    public DateTimeOffset GetLocalNow() => _innerContext.GetLocalNow();

    public void Log(string message) => _innerContext.Log(message);

    public void ReportProgress(int progress) => _innerContext.ReportProgress(progress);

    public string? GetVariable(string name) => _innerContext.GetVariable(name);

    public void SetVariable(string name, string value) => _innerContext.SetVariable(name, value);

    public string ResolvePath(string path) => _innerContext.ToAbsolutePath(path);

    public ValueTask PublishAsync(PluginUiRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var detail = string.IsNullOrWhiteSpace(request.Title)
            ? $"UI要求: {request.RequestType}"
            : $"UI要求: {request.RequestType} / {request.Title}";
        _innerContext.Log(detail);
        return ValueTask.CompletedTask;
    }

    public async ValueTask WriteArtifactAsync(PluginArtifactRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var absolutePath = _innerContext.ToAbsolutePath(request.RelativePath);
        var directoryPath = Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        if (request.BinaryContent is { Length: > 0 })
        {
            await File.WriteAllBytesAsync(absolutePath, request.BinaryContent, cancellationToken).ConfigureAwait(false);
            return;
        }

        await File.WriteAllTextAsync(
                absolutePath,
                request.TextContent ?? string.Empty,
                cancellationToken)
            .ConfigureAwait(false);
    }
}

