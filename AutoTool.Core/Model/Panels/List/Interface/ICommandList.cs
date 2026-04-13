using System.Collections.ObjectModel;

namespace AutoTool.Panels.Model.List.Interface;

internal interface ICommandList
{
    ObservableCollection<ICommandListItem> Items { get; }
    void Add(ICommandListItem item);
    void Remove(ICommandListItem item);
    void Clear();
    void Move(int oldIndex, int newIndex);
    void Copy(int oldIndex, int newIndex);
    void Save(string fileName);
    void Load(string fileName);
}
