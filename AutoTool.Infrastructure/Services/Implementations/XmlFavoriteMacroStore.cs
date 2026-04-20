using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using AutoTool.Application.Ports;
using AutoTool.Domain.Macros;

namespace AutoTool.Infrastructure.Implementations;

/// <summary>
/// 永続化データの保存と読み込みを担当します。
/// </summary>
public class XmlFavoriteMacroStore : IFavoriteMacroStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public ObservableCollection<FavoriteMacroEntry>? Load(string key)
    {
        var jsonFavorites = DeserializeJsonFromFile(key);
        if (jsonFavorites is not null)
        {
            return jsonFavorites;
        }

        var legacyXmlPath = Path.ChangeExtension(key, ".xml");
        var xmlFavorites = XmlFileSerializer.DeserializeFromFile<ObservableCollection<FavoriteMacroEntry>>(legacyXmlPath);
        if (xmlFavorites is null)
        {
            return null;
        }

        Save(key, xmlFavorites);
        TryDeleteLegacyFile(legacyXmlPath);
        return xmlFavorites;
    }

    public void Save(string key, ObservableCollection<FavoriteMacroEntry>? favorites)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(key)!);
        var json = JsonSerializer.Serialize(favorites ?? [], JsonOptions);
        File.WriteAllText(key, json);
    }

    private static ObservableCollection<FavoriteMacroEntry>? DeserializeJsonFromFile(string path)
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

            return JsonSerializer.Deserialize<ObservableCollection<FavoriteMacroEntry>>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
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
            // 削除に失敗しても旧ファイルは保持し、処理は継続します。
        }
    }
}
