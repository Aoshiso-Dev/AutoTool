using AutoTool.Plugin.Host.Abstractions;

namespace AutoTool.Desktop.Services;

public sealed class PluginStartupDiagnosticsPresenter(IPluginStartupDiagnosticsCatalog diagnosticsCatalog)
{
    private readonly IPluginStartupDiagnosticsCatalog _diagnosticsCatalog = diagnosticsCatalog ?? throw new ArgumentNullException(nameof(diagnosticsCatalog));

    public PluginStartupDiagnosticsPresentation BuildPresentation()
    {
        var diagnostics = _diagnosticsCatalog.GetDiagnostics();
        if (diagnostics.Count == 0)
        {
            return new PluginStartupDiagnosticsPresentation
            {
                StatusMessage = null,
                ShouldOpenLogPanel = false,
                Entries = [],
            };
        }

        List<PluginStartupDiagnosticsEntry> entries = [];
        foreach (var diagnostic in diagnostics.OrderBy(static x => x.DisplayName, StringComparer.Ordinal))
        {
            var headline = diagnostic.IsHealthy
                ? $"プラグインを読み込みました: {diagnostic.PluginId} ({diagnostic.DisplayName} v{diagnostic.Version})"
                : $"プラグイン読込で問題を検出しました: {diagnostic.PluginId} ({diagnostic.DisplayName} v{diagnostic.Version})";
            entries.Add(new PluginStartupDiagnosticsEntry
            {
                CommandName = "プラグイン",
                Message = headline,
            });

            if (!string.IsNullOrWhiteSpace(diagnostic.Summary))
            {
                entries.Add(new PluginStartupDiagnosticsEntry
                {
                    CommandName = "プラグイン",
                    Message = $"概要: {diagnostic.Summary}",
                });
            }

            if (diagnostic.RequestedPermissions.Count > 0)
            {
                entries.Add(new PluginStartupDiagnosticsEntry
                {
                    CommandName = "プラグイン",
                    Message = $"要求権限: {string.Join(", ", diagnostic.RequestedPermissions)}",
                });
            }

            if (diagnostic.MissingPermissions.Count > 0)
            {
                entries.Add(new PluginStartupDiagnosticsEntry
                {
                    CommandName = "プラグイン",
                    Message = $"不足権限: {string.Join(", ", diagnostic.MissingPermissions)}",
                });
            }

            foreach (var message in diagnostic.Messages)
            {
                entries.Add(new PluginStartupDiagnosticsEntry
                {
                    CommandName = "プラグイン",
                    Message = message,
                });
            }
        }

        var unhealthyCount = diagnostics.Count(static x => !x.IsHealthy);
        var healthyCount = diagnostics.Count - unhealthyCount;
        var statusMessage = unhealthyCount == 0
            ? $"プラグイン起動時診断: 正常 {healthyCount} 件"
            : $"プラグイン起動時診断: 異常 {unhealthyCount} 件 / 正常 {healthyCount} 件";

        return new PluginStartupDiagnosticsPresentation
        {
            StatusMessage = statusMessage,
            ShouldOpenLogPanel = unhealthyCount > 0,
            Entries = entries,
        };
    }
}

public sealed record PluginStartupDiagnosticsPresentation
{
    public string? StatusMessage { get; init; }

    public bool ShouldOpenLogPanel { get; init; }

    public IReadOnlyList<PluginStartupDiagnosticsEntry> Entries { get; init; } = [];
}

public sealed record PluginStartupDiagnosticsEntry
{
    public required string CommandName { get; init; }

    public required string Message { get; init; }
}

