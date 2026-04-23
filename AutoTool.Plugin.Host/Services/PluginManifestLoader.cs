using System.Text.Json;
using AutoTool.Plugin.Abstractions.PluginModel;
using AutoTool.Plugin.Host.Abstractions;
using AutoTool.Plugin.Host.Models;

namespace AutoTool.Plugin.Host.Services;

public sealed class PluginManifestLoader(IPluginManifestValidator validator) : IPluginManifestLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly IPluginManifestValidator _validator = validator ?? throw new ArgumentNullException(nameof(validator));

    public PluginManifestLoadResult Load(string manifestPath)
    {
        ArgumentNullException.ThrowIfNull(manifestPath);

        var pluginDirectoryPath = Path.GetDirectoryName(manifestPath) ?? string.Empty;
        List<string> errors = [];

        if (!File.Exists(manifestPath))
        {
            errors.Add($"plugin.json が見つかりません: {manifestPath}");
            return new PluginManifestLoadResult
            {
                ManifestPath = manifestPath,
                PluginDirectoryPath = pluginDirectoryPath,
                IsValid = false,
                Errors = errors,
            };
        }

        PluginManifest? manifest;
        try
        {
            var json = File.ReadAllText(manifestPath);
            manifest = JsonSerializer.Deserialize<PluginManifest>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            errors.Add($"plugin.json の JSON 解析に失敗しました: {ex.Message}");
            return new PluginManifestLoadResult
            {
                ManifestPath = manifestPath,
                PluginDirectoryPath = pluginDirectoryPath,
                IsValid = false,
                Errors = errors,
            };
        }
        catch (IOException ex)
        {
            errors.Add($"plugin.json の読み込みに失敗しました: {ex.Message}");
            return new PluginManifestLoadResult
            {
                ManifestPath = manifestPath,
                PluginDirectoryPath = pluginDirectoryPath,
                IsValid = false,
                Errors = errors,
            };
        }

        if (manifest is null)
        {
            errors.Add("plugin.json の内容が空です。");
            return new PluginManifestLoadResult
            {
                ManifestPath = manifestPath,
                PluginDirectoryPath = pluginDirectoryPath,
                IsValid = false,
                Errors = errors,
            };
        }

        errors.AddRange(_validator.Validate(manifest, pluginDirectoryPath));

        return new PluginManifestLoadResult
        {
            ManifestPath = manifestPath,
            PluginDirectoryPath = pluginDirectoryPath,
            Manifest = manifest,
            IsValid = errors.Count == 0,
            Errors = errors,
        };
    }
}

