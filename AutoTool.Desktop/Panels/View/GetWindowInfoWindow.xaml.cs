using System.Windows;
using AutoTool.Commands.Infrastructure;
using AutoTool.Commands.Threading;

namespace AutoTool.Desktop.Panels.View;

public partial class GetWindowInfoWindow : Window
{
    private bool _hookRegistered;
    private CancellationTokenSource? _hookSubscriptionCts;

    public string WindowTitle { get; set; } = string.Empty;
    public string WindowClassName { get; set; } = string.Empty;

    public GetWindowInfoWindow()
    {
        InitializeComponent();

        var screenCurrentPoint = Win32MouseInterop.GetCursorPosition();
        Left = screenCurrentPoint.X + 5;
        Top = screenCurrentPoint.Y + 5;

        StartHook();
    }

    private void StartHook()
    {
        if (_hookRegistered)
        {
            return;
        }

        _hookSubscriptionCts = new();
        _ = ConsumeHookEventsAsync(_hookSubscriptionCts.Token);
        Win32MouseHookHelper.StartHook();
        _hookRegistered = true;
    }

    private async Task ConsumeHookEventsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var ev in Win32MouseHookHelper.ReadEventsAsync().ConfigureAwaitFalse(cancellationToken))
            {
                switch (ev.Kind)
                {
                    case Win32MouseHookHelper.MouseHookEventKind.MouseMove:
                        await Dispatcher.InvokeAsync(OnMouseMove);
                        break;
                    case Win32MouseHookHelper.MouseHookEventKind.LButtonUp:
                        await Dispatcher.InvokeAsync(OnMouseUp);
                        return;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void OnMouseMove()
    {
        var screenCurrentPoint = Win32MouseInterop.GetCursorPosition();
        Left = screenCurrentPoint.X + 5;
        Top = screenCurrentPoint.Y - Height - 5;

        TextBlock_WindowTitle.Text = Win32WindowInfoHelper.GetWindowTitle(screenCurrentPoint);
        TextBlock_WindowClassName.Text = Win32WindowInfoHelper.GetWindowClassName(screenCurrentPoint);
    }

    private void OnMouseUp()
    {
        StopHook();

        WindowTitle = TextBlock_WindowTitle.Text;
        WindowClassName = TextBlock_WindowClassName.Text;

        DialogResult = true;
        Close();
    }

    private void StopHook()
    {
        if (!_hookRegistered)
        {
            return;
        }

        _hookSubscriptionCts?.Cancel();
        _hookSubscriptionCts?.Dispose();
        _hookSubscriptionCts = null;
        Win32MouseHookHelper.StopHook();
        _hookRegistered = false;
    }

    protected override void OnClosed(EventArgs e)
    {
        StopHook();
        base.OnClosed(e);
    }
}

