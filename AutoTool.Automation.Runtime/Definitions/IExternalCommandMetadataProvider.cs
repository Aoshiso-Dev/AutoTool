namespace AutoTool.Automation.Runtime.Definitions;

/// <summary>
/// 実行時に外部ソースからコマンド定義メタデータを供給します。
/// </summary>
public interface IExternalCommandMetadataProvider
{
    IReadOnlyList<CommandMetadata> GetCommandMetadata();
}

