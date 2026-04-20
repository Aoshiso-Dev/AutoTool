using System.Collections.ObjectModel;

namespace AutoTool.Application.Ports;

/// <summary>
/// 最近使ったファイル一覧の保存・読み込みを担うポートです。
/// </summary>
public interface IRecentFileStore
{
    /// <summary>保存済みの最近使ったファイル一覧を読み込みます。</summary>
    ObservableCollection<RecentFileEntry>? Load(string key);
    /// <summary>最近使ったファイル一覧を保存します。</summary>
    void Save(string key, ObservableCollection<RecentFileEntry>? files);
}

