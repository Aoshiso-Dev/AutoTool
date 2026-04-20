using System.Text.Json;
using System.Text.Json.Nodes;
using System.IO;
using AutoTool.Application.Ports;
using AutoTool.Infrastructure.Paths;

namespace AutoTool.Infrastructure.Implementations;

/// <summary>
/// 永続化データの保存と読み込みを担当します。
/// </summary>
public sealed class JsonUiStatePreferenceStore : IUiStatePreferenceStore
{
    private const string RestorePreviousSessionKey = "RestorePreviousSession";
    private const string UiStateSectionKey = "UiState";
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public bool LoadRestorePreviousSession()
    {
        try
        {
            var root = LoadRoot();
            if (root is null)
            {
                return true;
            }

            return root[UiStateSectionKey]?[RestorePreviousSessionKey]?.GetValue<bool>() ?? true;
        }
        catch
        {
            return true;
        }
    }

    public void SaveRestorePreviousSession(bool enabled)
    {
        var root = LoadRoot() ?? new JsonObject();
        var uiStateSection = root[UiStateSectionKey] as JsonObject ?? new JsonObject();

        uiStateSection[RestorePreviousSessionKey] = enabled;
        root[UiStateSectionKey] = uiStateSection;

        var settingsDirectory = GetSettingsDirectoryPath();
        Directory.CreateDirectory(settingsDirectory);
        File.WriteAllText(GetSettingsFilePath(), root.ToJsonString(SerializerOptions));
    }

    private static JsonObject? LoadRoot()
    {
        var settingsFilePath = GetSettingsFilePath();
        if (!File.Exists(settingsFilePath))
        {
            return null;
        }

        var content = File.ReadAllText(settingsFilePath);
        if (string.IsNullOrWhiteSpace(content))
        {
            return new JsonObject();
        }

        return JsonNode.Parse(content) as JsonObject;
    }

    private static string GetSettingsDirectoryPath()
    {
        return Path.Combine(ApplicationPathResolver.GetApplicationDirectory(), "Settings");
    }

    private static string GetSettingsFilePath()
    {
        return Path.Combine(GetSettingsDirectoryPath(), "appsettings.json");
    }
}
