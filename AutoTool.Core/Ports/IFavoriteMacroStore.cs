using System.Collections.ObjectModel;
using AutoTool.Model;

namespace AutoTool.Core.Ports;

public interface IFavoriteMacroStore
{
    ObservableCollection<FavoriteMacroEntry>? Load(string key);
    void Save(string key, ObservableCollection<FavoriteMacroEntry>? favorites);
}
