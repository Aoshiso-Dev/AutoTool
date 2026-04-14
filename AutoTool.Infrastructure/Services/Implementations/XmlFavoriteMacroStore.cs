using System.Collections.ObjectModel;
using AutoTool.Core.Ports;
using AutoTool.Model;

namespace AutoTool.Infrastructure.Implementations;

public class XmlFavoriteMacroStore : IFavoriteMacroStore
{
    public ObservableCollection<FavoriteMacroEntry>? Load(string key)
    {
        return XmlFileSerializer.DeserializeFromFile<ObservableCollection<FavoriteMacroEntry>>(key);
    }

    public void Save(string key, ObservableCollection<FavoriteMacroEntry>? favorites)
    {
        XmlFileSerializer.SerializeToFile(favorites, key);
    }
}
