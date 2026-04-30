using AutoTool.Plugin.Abstractions.Video;
using AutoTool.Plugin.Host.Models;

namespace AutoTool.Plugin.Host.Services;

public sealed class VideoStreamRegistry : IVideoStreamRegistry, IVideoStreamRegistryDiagnostics
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, VideoStreamRegistration> _registrations = new(StringComparer.Ordinal);
    private readonly List<VideoStreamRegistryIssue> _issues = [];

    public ValueTask RegisterAsync(
        VideoStreamRegistration registration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registration);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRegistration(registration);

        lock (_syncRoot)
        {
            if (_registrations.TryGetValue(registration.SourceId, out var existing))
            {
                var message =
                    $"映像ソース SourceId が重複しています: SourceId={registration.SourceId}, existingPluginId={existing.ProviderPluginId}, providerPluginId={registration.ProviderPluginId}";
                _issues.Add(new VideoStreamRegistryIssue
                {
                    SourceId = registration.SourceId,
                    ProviderPluginId = registration.ProviderPluginId,
                    Message = message,
                });
                throw new InvalidOperationException(message);
            }

            _registrations.Add(registration.SourceId, registration);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask UnregisterAsync(
        string sourceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            _registrations.Remove(sourceId);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<IVideoFrameSource?> GetSourceAsync(
        string sourceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            return ValueTask.FromResult(
                _registrations.TryGetValue(sourceId, out var registration)
                    ? registration.Source
                    : null);
        }
    }

    public IReadOnlyList<VideoStreamDescriptor> GetSources()
    {
        lock (_syncRoot)
        {
            return _registrations.Values
                .Select(static x => new VideoStreamDescriptor
                {
                    SourceId = x.SourceId,
                    DisplayName = x.DisplayName,
                    ProviderPluginId = x.ProviderPluginId,
                    Width = x.Width,
                    Height = x.Height,
                    PixelFormat = x.PixelFormat,
                })
                .OrderBy(static x => x.SourceId, StringComparer.Ordinal)
                .ToList();
        }
    }

    public IReadOnlyList<VideoStreamRegistryIssue> GetIssues()
    {
        lock (_syncRoot)
        {
            return _issues.ToList();
        }
    }

    public int GetRegisteredSourceCount(string providerPluginId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerPluginId);

        lock (_syncRoot)
        {
            return _registrations.Values.Count(x => string.Equals(
                x.ProviderPluginId,
                providerPluginId,
                StringComparison.Ordinal));
        }
    }

    private static void ValidateRegistration(VideoStreamRegistration registration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(registration.SourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(registration.DisplayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(registration.ProviderPluginId);
        ArgumentNullException.ThrowIfNull(registration.Source);

        if (!string.Equals(registration.SourceId, registration.Source.SourceId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"映像ソース登録の SourceId と IVideoFrameSource.SourceId が一致していません: registration={registration.SourceId}, source={registration.Source.SourceId}");
        }
    }
}
