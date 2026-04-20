using AutoTool.Automation.Contracts.Lists;

namespace AutoTool.Desktop.ViewModel;

/// <summary>
/// 関連データを不変に保持するレコード型です。
/// </summary>
public sealed record PreflightIssueItem(
    string Level,
    string Line,
    string CommandName,
    string PropertyName,
    string Message,
    int LineNumber = 0,
    ICommandListItem? CommandItem = null);
