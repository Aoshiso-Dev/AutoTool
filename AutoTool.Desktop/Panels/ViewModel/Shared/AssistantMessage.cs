using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoTool.Desktop.Panels.ViewModel.Shared;

/// <summary>
/// AI相談パネルに表示する1件の会話メッセージです。
/// </summary>
public sealed class AssistantMessage : ObservableObject
{
    private string _text = string.Empty;
    private bool _isPending;

    public required string Sender { get; init; }
    public required string Timestamp { get; init; }
    public bool IsUser { get; init; }

    public string Text
    {
        get => _text;
        set => SetProperty(ref _text, value);
    }

    public bool IsPending
    {
        get => _isPending;
        set => SetProperty(ref _isPending, value);
    }
}
