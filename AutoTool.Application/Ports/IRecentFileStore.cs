using System.Collections.ObjectModel;

namespace AutoTool.Application.Ports;

public interface IRecentFileStore
{
    ObservableCollection<RecentFileEntry>? Load(string key);
    void Save(string key, ObservableCollection<RecentFileEntry>? files);
}

