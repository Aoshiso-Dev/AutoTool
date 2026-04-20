using AutoTool.Automation.Contracts.Lists;

namespace AutoTool.Application.Ports;

/// <summary>
/// コマンド一覧の保存・読み込みを担う永続化ポートです。
/// </summary>
public interface ICommandListFileGateway
{
    /// <summary>コマンド一覧を指定パスへ保存します。</summary>
    void Save(IReadOnlyList<ICommandListItem> items, string filePath);
    /// <summary>指定パスからコマンド一覧を読み込みます。</summary>
    IReadOnlyList<ICommandListItem> Load(string filePath);
}
