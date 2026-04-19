using AutoTool.Application.Ports;
using AutoTool.Automation.Contracts.Lists;

namespace AutoTool.Application.Files;

public class CommandListFileUseCase(ICommandListFileGateway fileGateway)
{
    private readonly ICommandListFileGateway _fileGateway = fileGateway;

    public void Save(IEnumerable<ICommandListItem> items, string filePath)
    {
        ArgumentNullException.ThrowIfNull(items);

        var snapshot = items.Select(item => item.Clone()).ToList();
        _fileGateway.Save(snapshot, filePath);
    }

    public IReadOnlyList<ICommandListItem> Load(string filePath)
    {
        return _fileGateway.Load(filePath);
    }
}
