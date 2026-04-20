using System.Collections.ObjectModel;
using AutoTool.Domain.Macros;

namespace AutoTool.Application.Ports;

/// <summary>
/// お気に入りマクロ一覧の保存・読み込みを担うポートです。
/// </summary>
public interface IFavoriteMacroStore
{
    /// <summary>保存済みのお気に入り一覧を読み込みます。</summary>
    ObservableCollection<FavoriteMacroEntry>? Load(string key);
    /// <summary>お気に入り一覧を保存します。</summary>
    void Save(string key, ObservableCollection<FavoriteMacroEntry>? favorites);
}
