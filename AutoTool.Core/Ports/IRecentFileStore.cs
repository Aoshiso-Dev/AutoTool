using System.Collections.ObjectModel;
using AutoTool.Model;

namespace AutoTool.Core.Ports
{
    public interface IRecentFileStore
    {
        ObservableCollection<FileManager.RecentFile>? Load(string key);
        void Save(string key, ObservableCollection<FileManager.RecentFile>? files);
    }
}

