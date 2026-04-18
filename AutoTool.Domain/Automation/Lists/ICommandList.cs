using System.Collections.ObjectModel;
using AutoTool.Automation.Contracts.Lists;

namespace AutoTool.Domain.Automation.Lists;

public interface ICommandList
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