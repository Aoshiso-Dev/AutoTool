using System.Text.Json;
using AutoTool.Application.Assistant;
using AutoTool.Infrastructure.Paths;

namespace AutoTool.Infrastructure.Assistant;

/// <summary>
/// AI相談設定を Settings 配下のJSONファイルへ保存します。
/// </summary>
public sealed class JsonAssistantSettingsStore : IAssistantSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public AssistantSettings Load()
    {
        try
        {
            var path = GetSettingsFilePath();
            if (!File.Exists(path))
            {
                return new AssistantSettings();
            }

            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new AssistantSettings();
            }

            return (JsonSerializer.Deserialize<AssistantSettings>(json, SerializerOptions) ?? new AssistantSettings())
                .Normalize();
        }
        catch
        {
            return new AssistantSettings();
        }
    }

    public void Save(AssistantSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var normalized = settings.Normalize();
        var settingsDirectory = GetSettingsDirectoryPath();
        Directory.CreateDirectory(settingsDirectory);
        File.WriteAllText(GetSettingsFilePath(), JsonSerializer.Serialize(normalized, SerializerOptions));
    }

    private static string GetSettingsDirectoryPath()
    {
        return Path.Combine(ApplicationPathResolver.GetApplicationDirectory(), "Settings");
    }

    private static string GetSettingsFilePath()
    {
        return Path.Combine(GetSettingsDirectoryPath(), "assistant_settings.json");
    }
}
