using System.Collections.ObjectModel;
using AutoTool.Application.Ports;
using AutoTool.Domain.Macros;

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
