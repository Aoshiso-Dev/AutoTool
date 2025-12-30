using System.Collections.ObjectModel;
using AutoTool.Model;
using AutoTool.Services.Interfaces;

namespace AutoTool.Services.Implementations
{
    public class XmlRecentFileStore : IRecentFileStore
    {
        public ObservableCollection<FileManager.RecentFile>? Load(string key)
        {
            return XmlSerializer.XmlSerializer.DeserializeFromFile<ObservableCollection<FileManager.RecentFile>>(key);
        }

        public void Save(string key, ObservableCollection<FileManager.RecentFile>? files)
        {
            XmlSerializer.XmlSerializer.SerializeToFile(files, key);
        }
    }
}
