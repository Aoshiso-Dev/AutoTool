using System.Collections.ObjectModel;
using AutoTool.Domain.Macros;

namespace AutoTool.Application.Ports;

public interface IFavoriteMacroStore
{
    ObservableCollection<FavoriteMacroEntry>? Load(string key);
    void Save(string key, ObservableCollection<FavoriteMacroEntry>? favorites);
}