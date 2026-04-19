using AutoTool.Automation.Contracts.Lists;

namespace AutoTool.Application.Ports;

public interface ICommandListFileGateway
{
    void Save(IReadOnlyList<ICommandListItem> items, string filePath);
    IReadOnlyList<ICommandListItem> Load(string filePath);
}
