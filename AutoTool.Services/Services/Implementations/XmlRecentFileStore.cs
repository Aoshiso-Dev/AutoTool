using System.Collections.ObjectModel;
using AutoTool.Infrastructure;
using AutoTool.Model;
using AutoTool.Services.Interfaces;

namespace AutoTool.Services.Implementations
{
    public class XmlRecentFileStore : IRecentFileStore
    {
        public ObservableCollection<FileManager.RecentFile>? Load(string key)
        {
            return XmlFileSerializer.DeserializeFromFile<ObservableCollection<FileManager.RecentFile>>(key);
        }

        public void Save(string key, ObservableCollection<FileManager.RecentFile>? files)
        {
            XmlFileSerializer.SerializeToFile(files, key);
        }
    }
}
