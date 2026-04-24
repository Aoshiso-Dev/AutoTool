using AutoTool.Plugin.Abstractions.PluginModel;
using AutoTool.Plugin.Host.Abstractions;

namespace AutoTool.Plugin.Host.Services;

public sealed class PluginManifestValidator : IPluginManifestValidator
{
    public IReadOnlyList<string> Validate(PluginManifest manifest, string pluginDirectoryPath)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(pluginDirectoryPath);

        List<string> errors = [];

        ValidateRequired(manifest.PluginId, nameof(manifest.PluginId), errors);
        ValidateRequired(manifest.DisplayName, nameof(manifest.DisplayName), errors);
        ValidateRequired(manifest.Version, nameof(manifest.Version), errors);
        ValidateRequired(manifest.EntryAssembly, nameof(manifest.EntryAssembly), errors);
        ValidateRequired(manifest.EntryType, nameof(manifest.EntryType), errors);

        if (string.IsNullOrWhiteSpace(manifest.PluginId) is false &&
            manifest.PluginId.Any(char.IsWhiteSpace))
        {
            errors.Add("pluginId に空白文字は使用できません。");
        }

        if (string.IsNullOrWhiteSpace(manifest.EntryAssembly) is false)
        {
            if (Path.IsPathRooted(manifest.EntryAssembly))
            {
                errors.Add("entryAssembly は相対パスで指定してください。");
            }
            else
            {
                var assemblyPath = Path.Combine(pluginDirectoryPath, manifest.EntryAssembly);
                if (!File.Exists(assemblyPath))
                {
                    errors.Add($"entryAssembly が見つかりません: {manifest.EntryAssembly}");
                }
            }
        }

        if (manifest.Permissions.Count != manifest.Permissions.Distinct(StringComparer.Ordinal).Count())
        {
            errors.Add("permissions に重複があります。");
        }

        foreach (var command in manifest.Commands)
        {
            ValidateRequired(command.CommandType, "commands.commandType", errors);
            ValidateRequired(command.DisplayName, "commands.displayName", errors);
            ValidateRequired(command.Category, "commands.category", errors);
        }

        foreach (var quickAction in manifest.QuickActions)
        {
            ValidateRequired(quickAction.ActionId, "quickActions.actionId", errors);
            ValidateRequired(quickAction.DisplayName, "quickActions.displayName", errors);
            ValidateRequired(quickAction.CommandType, "quickActions.commandType", errors);

            if (string.IsNullOrWhiteSpace(quickAction.Location) is false &&
                !string.Equals(quickAction.Location, PluginQuickActionLocations.ExtensionToolbar, StringComparison.Ordinal))
            {
                errors.Add($"quickActions.location は {PluginQuickActionLocations.ExtensionToolbar} のみ指定できます。");
            }
        }

        var duplicateActionIds = manifest.QuickActions
            .Where(static x => !string.IsNullOrWhiteSpace(x.ActionId))
            .GroupBy(static x => x.ActionId, StringComparer.Ordinal)
            .Where(static x => x.Count() > 1)
            .Select(static x => x.Key);
        foreach (var actionId in duplicateActionIds)
        {
            errors.Add($"quickActions.actionId に重複があります: {actionId}");
        }

        return errors;
    }

    private static void ValidateRequired(string? value, string propertyName, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{propertyName} は必須です。");
        }
    }
}


