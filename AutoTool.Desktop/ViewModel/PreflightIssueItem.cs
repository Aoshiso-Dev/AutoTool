namespace AutoTool.Desktop.ViewModel;

public sealed record PreflightIssueItem(
    string Level,
    string Line,
    string CommandName,
    string PropertyName,
    string Message);
