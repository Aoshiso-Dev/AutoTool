using AutoTool.Plugin.Abstractions.PluginModel;
using AutoTool.Plugin.Host.Abstractions;
using AutoTool.Plugin.Host.Models;

namespace AutoTool.Plugin.Host.Services;

public sealed class PluginStartupDiagnosticsCatalog(
    ILoadedPluginCatalog loadedPluginCatalog,
    IPluginCommandCatalog pluginCommandCatalog) : IPluginStartupDiagnosticsCatalog
{
    private readonly ILoadedPluginCatalog _loadedPluginCatalog = loadedPluginCatalog ?? throw new ArgumentNullException(nameof(loadedPluginCatalog));
    private readonly IPluginCommandCatalog _pluginCommandCatalog = pluginCommandCatalog ?? throw new ArgumentNullException(nameof(pluginCommandCatalog));
    private readonly object _syncRoot = new();
    private IReadOnlyList<PluginStartupDiagnostics>? _cachedDiagnostics;

    public IReadOnlyList<PluginStartupDiagnostics> GetDiagnostics()
    {
        if (_cachedDiagnostics is not null)
        {
            return _cachedDiagnostics;
        }

        lock (_syncRoot)
        {
            if (_cachedDiagnostics is not null)
            {
                return _cachedDiagnostics;
            }

            var commandDefinitions = _pluginCommandCatalog.GetCommandDefinitions();
            _cachedDiagnostics = _loadedPluginCatalog.GetLoadedPlugins()
                .Select(plugin => CreateDiagnostics(plugin, commandDefinitions))
                .ToList();

            return _cachedDiagnostics;
        }
    }

    private static PluginStartupDiagnostics CreateDiagnostics(
        LoadedPlugin plugin,
        IReadOnlyList<PluginCommandDefinition> commandDefinitions)
    {
        var requestedPermissions = plugin.Manifest.Permissions
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToArray();
        var commandPermissions = commandDefinitions
            .Where(x => string.Equals(x.PluginId, plugin.Manifest.PluginId, StringComparison.Ordinal))
            .SelectMany(static x => x.RequiredPermissions)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToArray();
        var missingPermissions = commandPermissions
            .Except(requestedPermissions, StringComparer.Ordinal)
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToArray();

        var healthCheckResult = RunHealthCheck(plugin);
        List<string> messages = [];
        if (healthCheckResult?.Messages.Count > 0)
        {
            messages.AddRange(healthCheckResult.Messages);
        }

        if (missingPermissions.Length > 0)
        {
            messages.Add($"commands が要求する権限が permissions に不足しています: {string.Join(", ", missingPermissions)}");
        }

        var isHealthy = (healthCheckResult?.IsHealthy ?? true) && missingPermissions.Length == 0;
        var summary = BuildSummary(healthCheckResult, missingPermissions, isHealthy);

        return new PluginStartupDiagnostics
        {
            PluginId = plugin.Manifest.PluginId,
            DisplayName = plugin.Manifest.DisplayName,
            Version = plugin.Manifest.Version,
            IsHealthy = isHealthy,
            Summary = summary,
            RequestedPermissions = requestedPermissions,
            CommandPermissions = commandPermissions,
            MissingPermissions = missingPermissions,
            Messages = messages,
            HealthCheckResult = healthCheckResult,
        };
    }

    private static PluginHealthCheckResult? RunHealthCheck(LoadedPlugin plugin)
    {
        if (plugin.HealthCheck is null)
        {
            return null;
        }

        try
        {
            return plugin.HealthCheck.CheckHealthAsync(CancellationToken.None)
                .AsTask()
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            return new PluginHealthCheckResult
            {
                IsHealthy = false,
                Summary = "ヘルスチェック実行失敗",
                Messages = [$"ヘルスチェック実行中に例外が発生しました: {ex.Message}"],
            };
        }
    }

    private static string BuildSummary(PluginHealthCheckResult? healthCheckResult, IReadOnlyList<string> missingPermissions, bool isHealthy)
    {
        if (missingPermissions.Count > 0)
        {
            return "権限定義不足";
        }

        if (healthCheckResult?.Summary is { Length: > 0 } summary)
        {
            return summary;
        }

        return isHealthy ? "正常" : "異常";
    }
}

