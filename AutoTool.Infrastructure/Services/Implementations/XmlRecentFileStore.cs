using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using AutoTool.Application.Files;
using AutoTool.Application.Ports;
using AutoTool.Infrastructure;
using AutoTool.Infrastructure.Paths;

namespace AutoTool.Infrastructure.Implementations;

public class XmlRecentFileStore : IRecentFileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public ObservableCollection<RecentFileEntry>? Load(string key)
    {
        var currentPath = ResolveCurrentJsonPath(key);
        var currentEntries = DeserializeJsonFromFile(currentPath);
        if (currentEntries is not null)
        {
            return currentEntries;
        }

        var legacyCandidates = ResolveLegacyPaths(key);
        foreach (var legacyPath in legacyCandidates)
        {
            var legacyEntries = TryDeserializeLegacy(legacyPath);
            if (legacyEntries is null)
            {
                continue;
            }

            Save(key, legacyEntries);
            TryDeleteLegacyFile(legacyPath);
            return legacyEntries;
        }

        return null;
    }

    public void Save(string key, ObservableCollection<RecentFileEntry>? files)
    {
        var currentPath = ResolveCurrentJsonPath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(currentPath)!);
        SerializeJsonToFile(files ?? [], currentPath);
    }

    private static string ResolveCurrentJsonPath(string key)
    {
        var settingsDirectory = Path.Combine(ApplicationPathResolver.GetApplicationDirectory(), "Settings");
        return Path.Combine(settingsDirectory, $"{key}.json");
    }

    private static IReadOnlyList<string> ResolveLegacyPaths(string key)
    {
        var appDirectory = ApplicationPathResolver.GetApplicationDirectory();
        var settingsDirectory = Path.Combine(appDirectory, "Settings");

        return
        [
            Path.Combine(settingsDirectory, $"{key}.xml"),
            Path.Combine(appDirectory, key),
            Path.Combine(appDirectory, $"{key}.xml")
        ];
    }

    private static ObservableCollection<RecentFileEntry>? TryDeserializeLegacy(string legacyPath)
    {
        if (!File.Exists(legacyPath))
        {
            return null;
        }

        if (string.Equals(Path.GetExtension(legacyPath), ".xml", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrEmpty(Path.GetExtension(legacyPath)))
        {
            var xmlEntries = XmlFileSerializer.DeserializeFromFile<ObservableCollection<RecentFileEntry>>(legacyPath);
            if (xmlEntries is not null)
            {
                return xmlEntries;
            }
        }

        return DeserializeJsonFromFile(legacyPath);
    }

    private static ObservableCollection<RecentFileEntry>? DeserializeJsonFromFile(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            return JsonSerializer.Deserialize<ObservableCollection<RecentFileEntry>>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static void SerializeJsonToFile(ObservableCollection<RecentFileEntry> files, string path)
    {
        var json = JsonSerializer.Serialize(files, JsonOptions);
        File.WriteAllText(path, json);
    }

    private static void TryDeleteLegacyFile(string legacyPath)
    {
        try
        {
            if (File.Exists(legacyPath))
            {
                File.Delete(legacyPath);
            }
        }
        catch
        {
            // No-op: keep legacy file when deletion fails.
        }
    }
}