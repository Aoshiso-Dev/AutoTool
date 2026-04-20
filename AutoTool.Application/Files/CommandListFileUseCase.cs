using AutoTool.Application.Ports;
using AutoTool.Automation.Contracts.Lists;

namespace AutoTool.Application.Files;

/// <summary>
/// コマンド一覧の保存・読込ユースケースを提供します。
/// </summary>
public class CommandListFileUseCase(ICommandListFileGateway fileGateway)
{
    private readonly ICommandListFileGateway _fileGateway = fileGateway;

    /// <summary>
    /// 保存前に項目を複製してスナップショット化し、外部変更の影響を避けて保存します。
    /// </summary>
    public void Save(IEnumerable<ICommandListItem> items, string filePath)
    {
        ArgumentNullException.ThrowIfNull(items);

        var snapshot = items.Select(item => item.Clone()).ToList();
        _fileGateway.Save(snapshot, filePath);
    }

    /// <summary>
    /// 永続化済みのコマンド一覧を読み込みます。
    /// </summary>
    public IReadOnlyList<ICommandListItem> Load(string filePath)
    {
        return _fileGateway.Load(filePath);
    }
}
