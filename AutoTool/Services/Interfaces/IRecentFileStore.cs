using System.Collections.ObjectModel;
using AutoTool.Model;

namespace AutoTool.Services.Interfaces
{
    public interface IRecentFileStore
    {
        ObservableCollection<FileManager.RecentFile>? Load(string key);
        void Save(string key, ObservableCollection<FileManager.RecentFile>? files);
    }
}
