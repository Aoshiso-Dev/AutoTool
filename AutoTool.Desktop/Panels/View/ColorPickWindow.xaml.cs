using System.Drawing;
using System.Windows;
using System.Windows.Media;
using AutoTool.Commands.Infrastructure;
using AutoTool.Commands.Model.Input;
using AutoTool.Commands.Threading;

namespace AutoTool.Panels.View;

public partial class ColorPickWindow : Window
{
    private bool _hookRegistered;
    private CancellationTokenSource? _hookSubscriptionCts;

    public CommandColor? Color { get; private set; }

    public ColorPickWindow()
    {
        InitializeComponent();
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
                        await Dispatcher.InvokeAsync(OnLButtonUp);
                        return;
                    case Win32MouseHookHelper.MouseHookEventKind.RButtonUp:
                        await Dispatcher.InvokeAsync(OnRButtonUp);
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
        var cursorPos = Win32MouseInterop.GetCursorPosition();
        Left = cursorPos.X + 10;
        Top = cursorPos.Y + 10;

        Color = GetColorAt(cursorPos);
        ColorPreview.Fill = new SolidColorBrush(
            Color is { } c ? System.Windows.Media.Color.FromArgb(c.A, c.R, c.G, c.B) : Colors.Transparent);
    }

    private void OnLButtonUp()
    {
        StopHook();
        DialogResult = true;
        Close();
    }

    private void OnRButtonUp()
    {
        StopHook();
        Color = null;
        DialogResult = false;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        StopHook();
        base.OnClosed(e);
    }

    private static CommandColor GetColorAt(System.Drawing.Point cursorPos)
    {
        using var bitmap = new Bitmap(1, 1);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(cursorPos.X, cursorPos.Y, 0, 0, new System.Drawing.Size(1, 1));

        var drawingColor = bitmap.GetPixel(0, 0);
        return new CommandColor(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
    }
}