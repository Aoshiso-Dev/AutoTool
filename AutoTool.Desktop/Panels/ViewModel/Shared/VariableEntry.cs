namespace AutoTool.Desktop.Panels.ViewModel.Shared;

/// <summary>
/// 変数ビューに表示する1件分の状態を表します。
/// </summary>
public sealed class VariableEntry
{
    public required string Name { get; init; }
    public required string Value { get; init; }
    public required string UpdatedAt { get; init; }

    public string SearchableText => $"{Name} {Value} {UpdatedAt}";
}
