using System.Collections.ObjectModel;
using AutoTool.Infrastructure;
using AutoTool.Application.Files;
using AutoTool.Application.Ports;

namespace AutoTool.Infrastructure.Implementations;

public class XmlRecentFileStore : IRecentFileStore
{
    public ObservableCollection<RecentFileEntry>? Load(string key)
    {
        return XmlFileSerializer.DeserializeFromFile<ObservableCollection<RecentFileEntry>>(key);
    }

    public void Save(string key, ObservableCollection<RecentFileEntry>? files)
    {
        XmlFileSerializer.SerializeToFile(files, key);
    }
}

