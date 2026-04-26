using System.Text.Json;
using AutoTool.Application.Assistant;
using AutoTool.Infrastructure.Paths;

namespace AutoTool.Infrastructure.Assistant;

/// <summary>
/// AIマクロ生成履歴を Settings 配下のJSONファイルへ保存します。
/// </summary>
public sealed class JsonAssistantMacroGenerationHistoryStore : IAssistantMacroGenerationHistoryStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public IReadOnlyList<AssistantMacroGenerationHistoryEntry> Load()
    {
        try
        {
            var path = GetHistoryFilePath();
            if (!File.Exists(path))
            {
                return [];
            }

            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            return JsonSerializer.Deserialize<List<AssistantMacroGenerationHistoryEntry>>(json, SerializerOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public void Append(AssistantMacroGenerationHistoryEntry entry, int maxCount = 20)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var normalizedMaxCount = Math.Clamp(maxCount, 1, 100);
        var entries = Load()
            .Prepend(entry)
            .Take(normalizedMaxCount)
            .ToList();

        var settingsDirectory = GetSettingsDirectoryPath();
        Directory.CreateDirectory(settingsDirectory);
        File.WriteAllText(GetHistoryFilePath(), JsonSerializer.Serialize(entries, SerializerOptions));
    }

    private static string GetSettingsDirectoryPath()
    {
        return Path.Combine(ApplicationPathResolver.GetApplicationDirectory(), "Settings");
    }

    private static string GetHistoryFilePath()
    {
        return Path.Combine(GetSettingsDirectoryPath(), "assistant_macro_history.json");
    }
}
